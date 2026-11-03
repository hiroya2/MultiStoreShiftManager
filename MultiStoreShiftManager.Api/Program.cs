
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL接続設定
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "データベース接続文字列が設定されていません。");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// コントローラー
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// PostgreSQL接続テスト
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    var canConnect = await db.Database.CanConnectAsync();

    if (canConnect)
    {
        Console.WriteLine("PostgreSQLに接続できました。");
    }
    else
    {
        Console.WriteLine("PostgreSQLに接続できませんでした。");
    }
}

// HTTPリクエスト処理
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
