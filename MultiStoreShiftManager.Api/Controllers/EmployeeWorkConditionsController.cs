using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.DTOs;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeWorkConditionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeWorkConditionsController(AppDbContext context)
    {
        _context = context;
    }

    // 勤務条件の登録
    [HttpPost]
    public async Task<IActionResult> Create(
        EmployeeWorkConditionCreateRequest request)
    {
        // 従業員の存在と有効性を確認
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Id == request.EmployeeId &&
                e.IsActive &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            return BadRequest(
                "指定された従業員が存在しないか、無効です。");
        }

        // 同じ従業員の既存勤務条件を取得
        var existingConditions = await _context.EmployeeWorkConditions
            .Where(w => w.EmployeeId == request.EmployeeId)
            .ToListAsync();

        // 同じ適用開始日の条件が存在する場合は登録不可
        var sameStartDate = existingConditions.Any(w =>
            w.EffectiveStartDate == request.EffectiveStartDate);

        if (sameStartDate)
        {
            return Conflict(
                "同じ適用開始日の勤務条件がすでに登録されています。");
        }

        // 新条件より後に開始する将来条件との重複を確認
        var overlapsFutureCondition = existingConditions.Any(w =>
            w.EffectiveStartDate > request.EffectiveStartDate &&
            (!request.EffectiveEndDate.HasValue ||
             w.EffectiveStartDate <= request.EffectiveEndDate.Value));

        if (overlapsFutureCondition)
        {
            return Conflict(
                "将来の勤務条件と適用期間が重複するため登録できません。");
        }

        // 新条件の開始日時点で有効な、直前の勤務条件を取得
        var previousCondition = existingConditions
            .Where(w =>
                w.EffectiveStartDate < request.EffectiveStartDate &&
                (!w.EffectiveEndDate.HasValue ||
                 w.EffectiveEndDate.Value >= request.EffectiveStartDate))
            .OrderByDescending(w => w.EffectiveStartDate)
            .FirstOrDefault();

        // 直前の勤務条件を新条件の開始日前日で終了
        if (previousCondition != null)
        {
            previousCondition.EffectiveEndDate =
                request.EffectiveStartDate.AddDays(-1);

            previousCondition.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // 新しい勤務条件を作成
        var workCondition = new EmployeeWorkCondition
        {
            EmployeeId = request.EmployeeId,
            EffectiveStartDate = request.EffectiveStartDate,
            EffectiveEndDate = request.EffectiveEndDate,
            NightWorkAllowed = request.NightWorkAllowed,
            WeeklyMaxHours = request.WeeklyMaxHours,
            MonthlyMaxHours = request.MonthlyMaxHours,
            WeeklyTargetHours = request.WeeklyTargetHours,
            MonthlyTargetHours = request.MonthlyTargetHours,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.EmployeeWorkConditions.Add(workCondition);

        await _context.SaveChangesAsync();

        return StatusCode(201, workCondition);
    }

    // 勤務条件の一覧取得
    [HttpGet]
    public async Task<IActionResult> GetAll(
        int? employeeId = null,
        bool includeEnded = false)
    {
        var query = _context.EmployeeWorkConditions
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(w =>
                w.EmployeeId == employeeId.Value);
        }

        // 終了済みの勤務条件を除外
        if (!includeEnded)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            query = query.Where(w =>
                !w.EffectiveEndDate.HasValue ||
                w.EffectiveEndDate.Value >= today);
        }

        var conditions = await query
            .OrderBy(w => w.EmployeeId)
            .ThenBy(w => w.EffectiveStartDate)
            .Select(w => new
            {
                w.Id,
                w.EmployeeId,
                EmployeeName = w.Employee.EmployeeName,
                w.EffectiveStartDate,
                w.EffectiveEndDate,
                w.NightWorkAllowed,
                w.WeeklyMaxHours,
                w.MonthlyMaxHours,
                w.WeeklyTargetHours,
                w.MonthlyTargetHours,
                w.CreatedAt,
                w.UpdatedAt
            })
            .ToListAsync();

        return Ok(conditions);
    }

    // 勤務条件の変更（履歴を残して新規登録）
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        EmployeeWorkConditionUpdateRequest request)
    {
        var oldCondition = await _context.EmployeeWorkConditions
            .FirstOrDefaultAsync(w => w.Id == id);

        if (oldCondition == null)
        {
            return NotFound("指定された勤務条件が見つかりません。");
        }

        //既に終了日が設定されている勤務条件は変更不可
        if (oldCondition.EffectiveEndDate.HasValue)
        {
            return BadRequest(
                "終了済みの勤務条件は変更できません。新しい勤務条件を登録してください。");
        }

        // 過去の条件への遡及変更を防止
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (request.EffectiveStartDate < today)
        {
            return BadRequest(
                "過去の日付を適用開始日に指定することはできません。");
        }

        // 新条件は旧条件の開始日より後でなければならない
        if (request.EffectiveStartDate <= oldCondition.EffectiveStartDate)
        {
            return BadRequest(
                "新しい適用開始日は、変更前の開始日より後にしてください。");
        }

        //旧条件の適用終了日より後への変更を防止
        if (oldCondition.EffectiveEndDate.HasValue &&
            request.EffectiveStartDate > oldCondition.EffectiveEndDate.Value)
        {
            return BadRequest(
                "新しい適用開始日は、変更前の勤務条件の適用期間内に指定してください。");
        }

        // 他の勤務条件との期間重複を確認
        var hasOverlap = await _context.EmployeeWorkConditions
            .AnyAsync(w =>
                w.EmployeeId == oldCondition.EmployeeId &&
                w.Id != id &&
                (!w.EffectiveEndDate.HasValue ||
                 w.EffectiveEndDate.Value >= request.EffectiveStartDate) &&
                (!request.EffectiveEndDate.HasValue ||
                 w.EffectiveStartDate <= request.EffectiveEndDate.Value));

        if (hasOverlap)
        {
            return Conflict(
                "他の勤務条件と適用期間が重複しています。");
        }

        // 変更前の条件を新条件の前日で終了
        oldCondition.EffectiveEndDate =
            request.EffectiveStartDate.AddDays(-1);

        oldCondition.UpdatedAt = DateTimeOffset.UtcNow;

        // 新しい勤務条件を作成
        var newCondition = new EmployeeWorkCondition
        {
            EmployeeId = oldCondition.EmployeeId,
            EffectiveStartDate = request.EffectiveStartDate,
            EffectiveEndDate = request.EffectiveEndDate,
            NightWorkAllowed = request.NightWorkAllowed,
            WeeklyMaxHours = request.WeeklyMaxHours,
            MonthlyMaxHours = request.MonthlyMaxHours,
            WeeklyTargetHours = request.WeeklyTargetHours,
            MonthlyTargetHours = request.MonthlyTargetHours,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.EmployeeWorkConditions.Add(newCondition);

        await _context.SaveChangesAsync();

        return StatusCode(201, newCondition);
    }

    // 勤務条件の終了
    [HttpPut("{id}/end")]
    public async Task<IActionResult> End(
        int id,
        EmployeeWorkConditionEndRequest request)
    {
        // 対象の勤務条件を取得
        var workCondition = await _context.EmployeeWorkConditions
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workCondition == null)
        {
            return NotFound(
                "指定された勤務条件が見つかりません。");
        }

        // すでに終了日が設定されている場合は変更不可
        if (workCondition.EffectiveEndDate.HasValue)
        {
            return BadRequest(
                "この勤務条件には、すでに終了日が設定されています。");
        }

        // 終了日は開始日より前にできない
        if (request.EffectiveEndDate < workCondition.EffectiveStartDate)
        {
            return BadRequest(
                "適用終了日は適用開始日以降の日付にしてください。");
        }

        // 他の勤務条件との期間重複を確認
        var hasOverlap = await _context.EmployeeWorkConditions
            .AnyAsync(w =>
                w.EmployeeId == workCondition.EmployeeId &&
                w.Id != id &&
                w.EffectiveStartDate <= request.EffectiveEndDate &&
                (!w.EffectiveEndDate.HasValue ||
                 w.EffectiveEndDate.Value >= workCondition.EffectiveStartDate));

        if (hasOverlap)
        {
            return Conflict(
                "他の勤務条件と適用期間が重複するため、終了日を設定できません。");
        }

        // 終了日を設定
        workCondition.EffectiveEndDate = request.EffectiveEndDate;
        workCondition.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(workCondition);
    }
}