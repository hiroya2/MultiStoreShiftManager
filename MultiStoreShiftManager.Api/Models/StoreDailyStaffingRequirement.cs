
using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.Models;

public class StoreDailyStaffingRequirement
{
    public int Id { get; set; }

    // 対象店舗
    public int StoreId { get; set; }

    public Store? Store { get; set; }

    // 対象日
    public DateOnly RequirementDate { get; set; }

    // 人数を必要とする時間帯
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    // 翌日にまたがる時間帯か
    public bool EndsNextDay { get; set; }

    // 必要な最低人数
    [Range(1, 100)]
    public int MinimumStaffCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
