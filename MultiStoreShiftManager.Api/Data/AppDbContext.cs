
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeStoreAssignment> EmployeeStoreAssignments 
        => Set<EmployeeStoreAssignment>();

    public DbSet<EmployeeWorkCondition> EmployeeWorkConditions
        => Set<EmployeeWorkCondition>();

    public DbSet<EmployeeAvailableTime> EmployeeAvailableTimes
        => Set<EmployeeAvailableTime>();

    public DbSet<EmployeeWeeklyAvailability> EmployeeWeeklyAvailabilities
        => Set<EmployeeWeeklyAvailability>();

    public DbSet<EmployeeStoreSupportPreference> EmployeeStoreSupportPreferences
        => Set<EmployeeStoreSupportPreference>();

    public DbSet<EmployeeMonthlyPreference> EmployeeMonthlyPreferences
        => Set<EmployeeMonthlyPreference>();

    public DbSet<EmployeeShiftRequest> EmployeeShiftRequests
        => Set<EmployeeShiftRequest>();

    public DbSet<StoreStaffingRequirement> StoreStaffingRequirements
        => Set<StoreStaffingRequirement>();

    public DbSet<StoreDailyStaffingRequirement> StoreDailyStaffingRequirements
    => Set<StoreDailyStaffingRequirement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Store>()
            .HasIndex(s => s.StoreCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<EmployeeStoreAssignment>()
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeStoreAssignment>()
            .HasOne(a => a.Store)
            .WithMany()
            .HasForeignKey(a => a.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeStoreAssignment>()
            .HasIndex(a => new
            {
                a.EmployeeId,
                a.StoreId,
                a.StartDate
            })
            .IsUnique();

        // 従業員勤務条件の関連付け
        modelBuilder.Entity<EmployeeWorkCondition>()
            .HasOne(w => w.Employee)
            .WithMany()
            .HasForeignKey(w => w.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // 従業員ごとの適用開始日を一意にする
        modelBuilder.Entity<EmployeeWorkCondition>()
            .HasIndex(w => new
            {
                w.EmployeeId,
                w.EffectiveStartDate
            })
            .IsUnique();

        // 勤務時間の小数精度
        modelBuilder.Entity<EmployeeWorkCondition>()
            .Property(w => w.WeeklyMaxHours)
            .HasPrecision(5, 2);

        modelBuilder.Entity<EmployeeWorkCondition>()
            .Property(w => w.MonthlyMaxHours)
            .HasPrecision(6, 2);

        modelBuilder.Entity<EmployeeWorkCondition>()
            .Property(w => w.WeeklyTargetHours)
            .HasPrecision(5, 2);

        modelBuilder.Entity<EmployeeWorkCondition>()
            .Property(w => w.MonthlyTargetHours)
            .HasPrecision(6, 2);

        modelBuilder.Entity<EmployeeAvailableTime>()
            .HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeAvailableTime>()
            .HasIndex(t => new
            {
                t.EmployeeId,
                t.EffectiveStartDate,
                t.DayOfWeek
            });

        modelBuilder.Entity<EmployeeWeeklyAvailability>()
    .HasOne(w => w.Employee)
    .WithMany()
    .HasForeignKey(w => w.EmployeeId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeWeeklyAvailability>()
            .HasIndex(w => new
            {
                w.EmployeeId,
                w.EffectiveStartDate,
                w.DayOfWeek
            });

        //従業員ごとのフォロー先店舗設定
        modelBuilder.Entity<EmployeeStoreSupportPreference>()
            .HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeStoreSupportPreference>()
            .HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        //同じ従業員・フォロー先店舗の組み合わせは1件だけ
        modelBuilder.Entity<EmployeeStoreSupportPreference>()
            .HasIndex(p => new
            {
                p.EmployeeId,
                p.StoreId
            })
            .IsUnique();

        //月ごとの従業員希望情報の関連付け
        modelBuilder.Entity<EmployeeMonthlyPreference>()
            .HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        //同じ従業員・対象月・曜日の重複登録を防止
        modelBuilder.Entity<EmployeeMonthlyPreference>()
            .HasIndex(p => new
            {
                p.EmployeeId,
                p.TargetMonth,
                p.DayOfWeek
            })
            .IsUnique();

        // 日付単位の従業員希望情報の関連付け
        modelBuilder.Entity<EmployeeShiftRequest>()
            .HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // 同じ従業員・希望対象日の重複登録を防止
        modelBuilder.Entity<EmployeeShiftRequest>()
            .HasIndex(r => new
            {
                r.EmployeeId,
                r.RequestDate
            })
            .IsUnique();

        //店舗ごとの最低人数設定の関連付け
        modelBuilder.Entity<StoreStaffingRequirement>()
            .HasOne(r => r.Store)
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        //店舗・曜日・時間帯の設定を管理
        modelBuilder.Entity<StoreStaffingRequirement>()
            .HasIndex(r => new
            {
                r.StoreId,
                r.DayOfWeek,
                r.StartTime,
            });

        // 特定日ごとの最低人数設定の関連付け
        modelBuilder.Entity<StoreDailyStaffingRequirement>()
            .HasOne(r => r.Store)
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // 店舗・対象日・時間帯の検索用インデックス
        modelBuilder.Entity<StoreDailyStaffingRequirement>()
            .HasIndex(r => new
            {
                r.StoreId,
                r.RequirementDate,
                r.StartTime
            });
    }
}
