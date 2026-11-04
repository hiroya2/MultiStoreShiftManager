using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeStoreSupportPreferencesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeStoreSupportPreferencesController(AppDbContext context)
    {
        _context = context;
    }

    // 登録
    [HttpPost]
    public async Task<IActionResult> Create(
        EmployeeStoreSupportPreferenceCreateRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Id == request.EmployeeId &&
                e.IsActive &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            return BadRequest("指定された従業員が存在しないか、無効です。");
        }

        var store = await _context.Stores
            .FirstOrDefaultAsync(s =>
                s.Id == request.StoreId &&
                s.IsActive);

        if (store == null)
        {
            return BadRequest("指定された店舗が存在しないか、無効です。");
        }

        if (request.IsAvailable && !request.Priority.HasValue)
        {
            return BadRequest("フォロー可能な場合は優先度を設定してください。");
        }

        if (!request.IsAvailable && request.Priority.HasValue)
        {
            return BadRequest("フォロー不可の場合、優先度は設定できません。");
        }

        var exists = await _context.EmployeeStoreSupportPreferences
            .AnyAsync(p =>
                p.EmployeeId == request.EmployeeId &&
                p.StoreId == request.StoreId);

        if (exists)
        {
            return Conflict("この従業員と店舗の組み合わせは既に登録されています。");
        }

        var preference = new EmployeeStoreSupportPreference
        {
            EmployeeId = request.EmployeeId,
            StoreId = request.StoreId,
            IsAvailable = request.IsAvailable,
            Priority = request.Priority,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.EmployeeStoreSupportPreferences.Add(preference);
        await _context.SaveChangesAsync();

        return StatusCode(201, preference);
    }

    // 一覧取得
    [HttpGet]
    public async Task<IActionResult> GetAll(
        int? employeeId = null,
        int? storeId = null)
    {
        var query = _context.EmployeeStoreSupportPreferences
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId.Value);
        }

        var preferences = await query
            .OrderBy(p => p.StoreId)
            .ThenBy(p => p.Priority)
            .Select(p => new
            {
                p.Id,
                p.EmployeeId,
                EmployeeName = p.Employee.EmployeeName,
                p.StoreId,
                StoreName = p.Store.StoreName,
                p.IsAvailable,
                p.Priority,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(preferences);
    }

    // 更新
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        EmployeeStoreSupportPreferenceUpdateRequest request)
    {
        var preference = await _context.EmployeeStoreSupportPreferences
            .FirstOrDefaultAsync(p => p.Id == id);

        if (preference == null)
        {
            return NotFound("フォロー設定が見つかりません。");
        }

        if (request.IsAvailable && !request.Priority.HasValue)
        {
            return BadRequest("フォロー可能な場合は優先度を設定してください。");
        }

        if (!request.IsAvailable && request.Priority.HasValue)
        {
            return BadRequest("フォロー不可の場合、優先度は設定できません。");
        }

        preference.IsAvailable = request.IsAvailable;
        preference.Priority = request.IsAvailable
            ? request.Priority
            : null;
        preference.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(preference);
    }
}