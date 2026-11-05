
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiStoreShiftManager.Api.Data;
using MultiStoreShiftManager.Api.Models;

namespace MultiStoreShiftManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreDailyStaffingRequirementsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StoreDailyStaffingRequirementsController(AppDbContext context)
    {
        _context = context;
    }

    // 特定日ごとの最低人数設定一覧取得
    [HttpGet]
    public async Task<IActionResult> GetRequirements(
        [FromQuery] int? storeId = null,
        [FromQuery] DateOnly? requirementDate = null)
    {
        var query = _context.StoreDailyStaffingRequirements
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId.Value);
        }

        if (requirementDate.HasValue)
        {
            query = query.Where(r =>
                r.RequirementDate == requirementDate.Value);
        }

        var requirements = await query
            .OrderBy(r => r.StoreId)
            .ThenBy(r => r.RequirementDate)
            .ThenBy(r => r.StartTime)
            .ToListAsync();

        return Ok(requirements);
    }

    // 特定日の最低人数設定登録
    [HttpPost]
    public async Task<IActionResult> CreateRequirement(
        StoreDailyStaffingRequirement requirement)
    {
        // 日付チェック
        if (requirement.RequirementDate == default)
        {
            return BadRequest(new
            {
                message = "対象日を指定してください。"
            });
        }

        // 時間帯チェック
        if (requirement.StartTime == requirement.EndTime)
        {
            return BadRequest(new
            {
                message = "開始時刻と終了時刻を異なる時刻にしてください。"
            });
        }

        if (!requirement.EndsNextDay &&
            requirement.EndTime <= requirement.StartTime)
        {
            return BadRequest(new
            {
                message = "翌日にまたがらない場合、終了時刻は開始時刻より後にしてください。"
            });
        }

        if (requirement.EndsNextDay &&
            requirement.EndTime >= requirement.StartTime)
        {
            return BadRequest(new
            {
                message = "翌日にまたがる場合、終了時刻は開始時刻より前にしてください。"
            });
        }

        // 最低人数チェック
        if (requirement.MinimumStaffCount < 1)
        {
            return BadRequest(new
            {
                message = "最低人数は1人以上で指定してください。"
            });
        }

        // 6:30～22:00と重なる時間帯は最低2人必要
        var start = requirement.StartTime.ToTimeSpan();
        var end = requirement.EndTime.ToTimeSpan();

        var intervals = requirement.EndsNextDay
            ? new[]
            {
                (Start: start, End: TimeSpan.FromDays(1)),
                (Start: TimeSpan.Zero, End: end)
            }
            : new[]
            {
                (Start: start, End: end)
            };

        var dayMinimumInterval = (
            Start: new TimeSpan(6, 30, 0),
            End: new TimeSpan(22, 0, 0)
        );

        var overlapsDayMinimum = intervals.Any(interval =>
            interval.Start < dayMinimumInterval.End &&
            interval.End > dayMinimumInterval.Start);

        if (overlapsDayMinimum &&
            requirement.MinimumStaffCount < 2)
        {
            return BadRequest(new
            {
                message = "6:30～22:00と重なる時間帯は最低2人以上にしてください。"
            });
        }

        // 対象店舗の存在確認
        var storeExists = await _context.Stores
            .AnyAsync(s =>
                s.Id == requirement.StoreId &&
                s.IsActive);

        if (!storeExists)
        {
            return BadRequest(new
            {
                message = "指定された有効な店舗が見つかりません。"
            });
        }

        // 同じ店舗・対象日・開始時刻の重複確認
        var exists = await _context.StoreDailyStaffingRequirements
            .AnyAsync(r =>
                r.StoreId == requirement.StoreId &&
                r.RequirementDate == requirement.RequirementDate &&
                r.StartTime == requirement.StartTime);

        if (exists)
        {
            return Conflict(new
            {
                message = "同じ店舗・対象日・開始時刻の設定が既に登録されています。"
            });
        }

        requirement.CreatedAt = DateTimeOffset.UtcNow;

        _context.StoreDailyStaffingRequirements.Add(requirement);

        await _context.SaveChangesAsync();

        return Ok(requirement);
    }

    // 特定日の最低人数設定を更新
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRequirement(
        int id,
        StoreDailyStaffingRequirement updatedRequirement)
    {
        var requirement = await _context.StoreDailyStaffingRequirements
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requirement == null)
        {
            return NotFound(new
            {
                message = "指定された最低人数設定が見つかりません。"
            });
        }

        // 日付チェック
        if (updatedRequirement.RequirementDate == default)
        {
            return BadRequest(new
            {
                message = "対象日を指定してください。"
            });
        }

        // 時間帯チェック
        if (updatedRequirement.StartTime == updatedRequirement.EndTime)
        {
            return BadRequest(new
            {
                message = "開始時刻と終了時刻を異なる時刻にしてください。"
            });
        }

        if (!updatedRequirement.EndsNextDay &&
            updatedRequirement.EndTime <= updatedRequirement.StartTime)
        {
            return BadRequest(new
            {
                message = "翌日にまたがらない場合、終了時刻は開始時刻より後にしてください。"
            });
        }

        if (updatedRequirement.EndsNextDay &&
            updatedRequirement.EndTime >= updatedRequirement.StartTime)
        {
            return BadRequest(new
            {
                message = "翌日にまたがる場合、終了時刻は開始時刻より前にしてください。"
            });
        }

        // 最低人数チェック
        if (updatedRequirement.MinimumStaffCount < 1)
        {
            return BadRequest(new
            {
                message = "最低人数は1人以上で指定してください。"
            });
        }

        // 6:30～22:00と重なる時間帯は最低2人必要
        var start = updatedRequirement.StartTime.ToTimeSpan();
        var end = updatedRequirement.EndTime.ToTimeSpan();

        var intervals = updatedRequirement.EndsNextDay
            ? new[]
            {
                (Start: start, End: TimeSpan.FromDays(1)),
                (Start: TimeSpan.Zero, End: end)
            }
            : new[]
            {
                (Start: start, End: end)
            };

        var dayMinimumInterval = (
            Start: new TimeSpan(6, 30, 0),
            End: new TimeSpan(22, 0, 0)
        );

        var overlapsDayMinimum = intervals.Any(interval =>
            interval.Start < dayMinimumInterval.End &&
            interval.End > dayMinimumInterval.Start);

        if (overlapsDayMinimum &&
            updatedRequirement.MinimumStaffCount < 2)
        {
            return BadRequest(new
            {
                message = "6:30～22:00と重なる時間帯は最低2人以上にしてください。"
            });
        }

        // 対象店舗の存在確認
        var storeExists = await _context.Stores
            .AnyAsync(s =>
                s.Id == updatedRequirement.StoreId &&
                s.IsActive);

        if (!storeExists)
        {
            return BadRequest(new
            {
                message = "指定された有効な店舗が見つかりません。"
            });
        }

        // 他の設定との重複確認
        var exists = await _context.StoreDailyStaffingRequirements
            .AnyAsync(r =>
                r.Id != id &&
                r.StoreId == updatedRequirement.StoreId &&
                r.RequirementDate == updatedRequirement.RequirementDate &&
                r.StartTime == updatedRequirement.StartTime);

        if (exists)
        {
            return Conflict(new
            {
                message = "同じ店舗・対象日・開始時刻の設定が既に登録されています。"
            });
        }

        // 設定を更新
        requirement.StoreId = updatedRequirement.StoreId;
        requirement.RequirementDate = updatedRequirement.RequirementDate;
        requirement.StartTime = updatedRequirement.StartTime;
        requirement.EndTime = updatedRequirement.EndTime;
        requirement.EndsNextDay = updatedRequirement.EndsNextDay;
        requirement.MinimumStaffCount = updatedRequirement.MinimumStaffCount;
        requirement.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(requirement);
    }

    // 特定日の最低人数設定を削除
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRequirement(int id)
    {
        var requirement = await _context.StoreDailyStaffingRequirements
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requirement == null)
        {
            return NotFound(new
            {
                message = "指定された最低人数設定が見つかりません。"
            });
        }

        _context.StoreDailyStaffingRequirements.Remove(requirement);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "特定日の最低人数設定を削除しました。"
        });
    }
}
