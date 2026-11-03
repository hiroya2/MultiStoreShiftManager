using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeWeeklyAvailabilitiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeWeeklyAvailabilitiesController(AppDbContext context)
    {
        _context = context;
    }

    // 曜日ごとの希望を登録
    [HttpPost]
    public async Task<IActionResult> Create(
        EmployeeWeeklyAvailabilityCreateRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Id == request.EmployeeId &&
                e.IsActive &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            return BadRequest("有効な従業員が見つかりません。");
        }

        // 同じ従業員・同じ曜日で適用期間が重なるデータを確認
        var overlaps = await _context.EmployeeWeeklyAvailabilities
            .AnyAsync(w =>
                w.EmployeeId == request.EmployeeId &&
                w.DayOfWeek == request.DayOfWeek &&
                w.EffectiveStartDate <=
                    (request.EffectiveEndDate ?? DateOnly.MaxValue) &&
                (!w.EffectiveEndDate.HasValue ||
                 w.EffectiveEndDate.Value >= request.EffectiveStartDate));

        if (overlaps)
        {
            return Conflict(
                "同じ従業員・曜日の適用期間が重複しています。");
        }

        var entity = new EmployeeWeeklyAvailability
        {
            EmployeeId = request.EmployeeId,
            DayOfWeek = request.DayOfWeek,
            AvailabilityType = request.AvailabilityType,
            EffectiveStartDate = request.EffectiveStartDate,
            EffectiveEndDate = request.EffectiveEndDate
        };

        _context.EmployeeWeeklyAvailabilities.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Get),
            new { employeeId = entity.EmployeeId },
            entity);
    }

    // 曜日ごとの希望を取得
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? employeeId,
        [FromQuery] bool includeEnded = false)
    {
        var query = _context.EmployeeWeeklyAvailabilities
            .Include(w => w.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(w =>
                w.EmployeeId == employeeId.Value);
        }

        if (!includeEnded)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            query = query.Where(w =>
                !w.EffectiveEndDate.HasValue ||
                w.EffectiveEndDate.Value >= today);
        }

        var result = await query
            .OrderBy(w => w.EmployeeId)
            .ThenBy(w => w.DayOfWeek)
            .ThenBy(w => w.EffectiveStartDate)
            .Select(w => new
            {
                w.Id,
                w.EmployeeId,
                EmployeeName = w.Employee.EmployeeName,
                w.DayOfWeek,
                w.AvailabilityType,
                w.EffectiveStartDate,
                w.EffectiveEndDate
            })
            .ToListAsync();

        return Ok(result);
    }

    // 曜日ごとの希望を更新
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        EmployeeWeeklyAvailabilityUpdateRequest request)
    {
        var availability = await _context.EmployeeWeeklyAvailabilities
            .FirstOrDefaultAsync(w => w.Id == id);

        if (availability == null)
        {
            return NotFound("曜日ごとの希望が見つかりません。");
        }

        // 更新対象以外で、同じ従業員・曜日の適用期間が重なるデータを確認
        var overlaps = await _context.EmployeeWeeklyAvailabilities
            .AnyAsync(w =>
                w.EmployeeId == availability.EmployeeId &&
                w.DayOfWeek == request.DayOfWeek &&
                w.Id != id &&
                w.EffectiveStartDate <=
                    (request.EffectiveEndDate ?? DateOnly.MaxValue) &&
                (!w.EffectiveEndDate.HasValue ||
                 w.EffectiveEndDate.Value >= request.EffectiveStartDate));

        if (overlaps)
        {
            return Conflict(
                "同じ従業員・曜日の適用期間が重複しています。");
        }

        availability.DayOfWeek = request.DayOfWeek;
        availability.AvailabilityType = request.AvailabilityType;
        availability.EffectiveStartDate = request.EffectiveStartDate;
        availability.EffectiveEndDate = request.EffectiveEndDate;
        availability.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(availability);
    }

    // 曜日ごとの希望に適用終了日を設定
    [HttpPut("{id}/end")]
    public async Task<IActionResult> End(
        int id,
        EmployeeWeeklyAvailabilityEndRequest request)
    {
        var availability = await _context.EmployeeWeeklyAvailabilities
            .FirstOrDefaultAsync(w => w.Id == id);

        if (availability == null)
        {
            return NotFound("曜日ごとの希望が見つかりません。");
        }

        if (request.EffectiveEndDate < availability.EffectiveStartDate)
        {
            return BadRequest(
                "適用終了日は適用開始日より前にできません。");
        }

        if (availability.EffectiveEndDate.HasValue)
        {
            return Conflict(
                "この曜日ごとの希望には既に終了日が設定されています。");
        }

        availability.EffectiveEndDate = request.EffectiveEndDate;
        availability.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(availability);
    }
}