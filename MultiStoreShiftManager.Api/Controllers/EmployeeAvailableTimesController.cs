using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeAvailableTimesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeAvailableTimesController(AppDbContext context)
    {
        _context = context;
    }

    // 勤務可能時間帯を登録
    [HttpPost]
    public async Task<IActionResult> Create(
        EmployeeAvailableTimeCreateRequest request)
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

        // 適用期間が重なる既存の勤務可能時間帯を取得
        var existingTimes = await _context.EmployeeAvailableTimes
            .Where(t =>
                t.EmployeeId == request.EmployeeId &&
                t.EffectiveStartDate <=
                    (request.EffectiveEndDate ?? DateOnly.MaxValue) &&
                (!t.EffectiveEndDate.HasValue ||
                 t.EffectiveEndDate.Value >= request.EffectiveStartDate))
            .ToListAsync();

        // 1週間を分単位で表す
        const int minutesPerDay = 24 * 60;
        const int minutesPerWeek = 7 * minutesPerDay;

        // 新しい時間帯を1週間の時間軸に変換
        int newStart =
            (request.DayOfWeek - 1) * minutesPerDay +
            request.StartTime.Hour * 60 +
            request.StartTime.Minute;

        int newEnd =
            (request.DayOfWeek - 1) * minutesPerDay +
            request.EndTime.Hour * 60 +
            request.EndTime.Minute;

        if (request.EndsNextDay)
        {
            newEnd += minutesPerDay;
        }

        // 既存の時間帯との重複チェック
        foreach (var existing in existingTimes)
        {
            int existingStart =
                (existing.DayOfWeek - 1) * minutesPerDay +
                existing.StartTime.Hour * 60 +
                existing.StartTime.Minute;

            int existingEnd =
                (existing.DayOfWeek - 1) * minutesPerDay +
                existing.EndTime.Hour * 60 +
                existing.EndTime.Minute;

            if (existing.EndsNextDay)
            {
                existingEnd += minutesPerDay;
            }

            bool overlaps = false;

            foreach (int weekOffset in new[] {
        -minutesPerWeek, 0, minutesPerWeek
    })
            {
                int shiftedStart = existingStart + weekOffset;
                int shiftedEnd = existingEnd + weekOffset;

                if (newStart < shiftedEnd &&
                    shiftedStart < newEnd)
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                return Conflict(
                    "同じ従業員の勤務可能時間帯と時間が重複しています。");
            }
        }

        var entity = new EmployeeAvailableTime
        {
            EmployeeId = request.EmployeeId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            EndsNextDay = request.EndsNextDay,
            EffectiveStartDate = request.EffectiveStartDate,
            EffectiveEndDate = request.EffectiveEndDate
        };

        _context.EmployeeAvailableTimes.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Get),
            new { id = entity.Id },
            entity);
    }

    // 勤務可能時間帯を取得
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? employeeId,
        [FromQuery] bool includeEnded = false)
    {
        var query = _context.EmployeeAvailableTimes
            .Include(t => t.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(t =>
                t.EmployeeId == employeeId.Value);
        }

        if (!includeEnded)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            query = query.Where(t =>
                !t.EffectiveEndDate.HasValue ||
                t.EffectiveEndDate.Value >= today);
        }

        var result = await query
            .OrderBy(t => t.EmployeeId)
            .ThenBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .Select(t => new
            {
                t.Id,
                t.EmployeeId,
                EmployeeName = t.Employee.EmployeeName,
                t.DayOfWeek,
                t.StartTime,
                t.EndTime,
                t.EndsNextDay,
                t.EffectiveStartDate,
                t.EffectiveEndDate
            })
            .ToListAsync();

        return Ok(result);
    }

    // 勤務可能時間帯の適用終了日を設定
    [HttpPut("{id}/end")]
    public async Task<IActionResult> End(
        int id,
        EmployeeAvailableTimeEndRequest request)
    {
        var availableTime = await _context.EmployeeAvailableTimes
            .FirstOrDefaultAsync(t => t.Id == id);

        if (availableTime == null)
        {
            return NotFound("勤務可能時間帯が見つかりません。");
        }

        if (request.EffectiveEndDate < availableTime.EffectiveStartDate)
        {
            return BadRequest("適用終了日は適用開始日より前にできません。");
        }

        if (availableTime.EffectiveEndDate.HasValue)
        {
            return Conflict("この勤務可能時間帯には既に終了日が設定されています。");
        }

        availableTime.EffectiveEndDate = request.EffectiveEndDate;
        availableTime.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(availableTime);
    }

    // 勤務可能時間帯を更新
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        EmployeeAvailableTimeUpdateRequest request)
    {
        var availableTime = await _context.EmployeeAvailableTimes
            .FirstOrDefaultAsync(t => t.Id == id);

        if (availableTime == null)
        {
            return NotFound("勤務可能時間帯が見つかりません。");
        }

        // 適用期間が重なる他の勤務可能時間帯を取得
        var existingTimes = await _context.EmployeeAvailableTimes
            .Where(t =>
                t.EmployeeId == availableTime.EmployeeId &&
                t.Id != id &&
                t.EffectiveStartDate <=
                    (request.EffectiveEndDate ?? DateOnly.MaxValue) &&
                (!t.EffectiveEndDate.HasValue ||
                 t.EffectiveEndDate.Value >= request.EffectiveStartDate))
            .ToListAsync();

        const int minutesPerDay = 24 * 60;
        const int minutesPerWeek = 7 * minutesPerDay;

        int newStart =
            (request.DayOfWeek - 1) * minutesPerDay +
            request.StartTime.Hour * 60 +
            request.StartTime.Minute;

        int newEnd =
            (request.DayOfWeek - 1) * minutesPerDay +
            request.EndTime.Hour * 60 +
            request.EndTime.Minute;

        if (request.EndsNextDay)
        {
            newEnd += minutesPerDay;
        }

        foreach (var existing in existingTimes)
        {
            int existingStart =
                (existing.DayOfWeek - 1) * minutesPerDay +
                existing.StartTime.Hour * 60 +
                existing.StartTime.Minute;

            int existingEnd =
                (existing.DayOfWeek - 1) * minutesPerDay +
                existing.EndTime.Hour * 60 +
                existing.EndTime.Minute;

            if (existing.EndsNextDay)
            {
                existingEnd += minutesPerDay;
            }

            foreach (int weekOffset in new[]
            {
            -minutesPerWeek, 0, minutesPerWeek
        })
            {
                int shiftedStart = existingStart + weekOffset;
                int shiftedEnd = existingEnd + weekOffset;

                if (newStart < shiftedEnd &&
                    shiftedStart < newEnd)
                {
                    return Conflict(
                        "同じ従業員の勤務可能時間帯と時間が重複しています。");
                }
            }
        }

        // 更新
        availableTime.DayOfWeek = request.DayOfWeek;
        availableTime.StartTime = request.StartTime;
        availableTime.EndTime = request.EndTime;
        availableTime.EndsNextDay = request.EndsNextDay;
        availableTime.EffectiveStartDate = request.EffectiveStartDate;
        availableTime.EffectiveEndDate = request.EffectiveEndDate;
        availableTime.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(availableTime);
    }
}