using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeMonthlyPreferencesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeMonthlyPreferencesController(AppDbContext context)
    {
        _context = context;
    }

    // 月ごとの希望情報を登録
    [HttpPost]
    public async Task<IActionResult> Create(
        EmployeeMonthlyPreferenceCreateRequest request)
    {
        if (request.TargetMonth.Day != 1)
        {
            return BadRequest("対象年月は毎月1日を指定してください。");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Id == request.EmployeeId &&
                e.IsActive &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            return BadRequest("指定された従業員が存在しないか、無効です。");
        }

        var exists = await _context.EmployeeMonthlyPreferences
            .AnyAsync(p =>
                p.EmployeeId == request.EmployeeId &&
                p.TargetMonth == request.TargetMonth &&
                p.DayOfWeek == request.DayOfWeek);

        if (exists)
        {
            return Conflict("同じ従業員・対象月・曜日の希望情報が既に登録されています。");
        }

        var preference = new EmployeeMonthlyPreference
        {
            EmployeeId = request.EmployeeId,
            TargetMonth = request.TargetMonth,
            DayOfWeek = request.DayOfWeek,
            AvailabilityType = request.AvailabilityType
        };

        _context.EmployeeMonthlyPreferences.Add(preference);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = preference.Id },
            preference);
    }

    // 希望情報を1件取得
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var preference = await _context.EmployeeMonthlyPreferences
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (preference == null)
        {
            return NotFound("希望情報が見つかりません。");
        }

        return Ok(preference);
    }

    // 希望情報を一覧取得
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] DateOnly? targetMonth)
    {
        var query = _context.EmployeeMonthlyPreferences
            .Include(p => p.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        }

        if (targetMonth.HasValue)
        {
            query = query.Where(p => p.TargetMonth == targetMonth.Value);
        }

        var preferences = await query
            .OrderBy(p => p.TargetMonth)
            .ThenBy(p => p.EmployeeId)
            .ThenBy(p => p.DayOfWeek)
            .ToListAsync();

        return Ok(preferences);
    }

    // 希望区分を更新
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        EmployeeMonthlyPreferenceUpdateRequest request)
    {
        var preference = await _context.EmployeeMonthlyPreferences
            .FirstOrDefaultAsync(p => p.Id == id);

        if (preference == null)
        {
            return NotFound("希望情報が見つかりません。");
        }

        preference.AvailabilityType = request.AvailabilityType;
        preference.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(preference);
    }
}