using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public StoresController(AppDbContext context)
    {
        _context = context;
    }

    // 店舗一覧取得
    [HttpGet]
    public async Task<IActionResult> GetStores(
        [FromQuery] bool includeInactive = false)
    {
        var query = _context.Stores.AsQueryable();

        //向こう店舗を含めない場合は有効店舗だけ取得
        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var stores = await query
            .OrderBy(s => s.Id)
            .ToListAsync();

        return Ok(stores);
    }

    // 店舗登録
    [HttpPost]
    public async Task<IActionResult> CreateStore(Store store)
    {
        // 同じ店舗コードが登録済みか確認
        var exists = await _context.Stores
            .AnyAsync(s => s.StoreCode == store.StoreCode);

        if (exists)
        {
            return Conflict(new
            {
                message = "この店舗コードは既に登録されています。"
            });
        }

        // 登録日時を設定
        store.CreatedAt = DateTimeOffset.UtcNow;

        _context.Stores.Add(store);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetStores),
            new { id = store.Id },
            store);
    }

    // 店舗情報更新
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStore(
        int id,
        Store updatedStore)
    {
        // 更新対象の店舗を取得
        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == id);

        if (store == null)
        {
            return NotFound(new
            {
                message = "指定された店舗が見つかりません。"
            });
        }

        // 他の店舗と店舗コードが重複していないか確認
        var exists = await _context.Stores
            .AnyAsync(s =>
                s.StoreCode == updatedStore.StoreCode &&
                s.Id != id);

        if (exists)
        {
            return Conflict(new
            {
                message = "この店舗コードは既に登録されています。"
            });
        }

        // 店舗情報を更新
        store.StoreCode = updatedStore.StoreCode;
        store.StoreName = updatedStore.StoreName;

        await _context.SaveChangesAsync();

        return Ok(store);
    }

    // 店舗の論理削除
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStore(int id)
    {
        // 削除対象の店舗を取得
        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == id);

        if (store == null)
        {
            return NotFound(new
            {
                message = "指定された店舗が見つかりません。"
            });
        }

        // 既に無効な店舗か確認
        if (!store.IsActive)
        {
            return BadRequest(new
            {
                message = "この店舗は既に無効になっています。"
            });
        }

        // データを削除せず、無効化する
        store.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "店舗を無効化しました。",
            store
        });
    }
}