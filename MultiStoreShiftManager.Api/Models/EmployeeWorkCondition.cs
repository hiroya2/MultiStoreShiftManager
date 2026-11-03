
using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.Models;

public class EmployeeWorkCondition
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    // 勤務条件の適用期間
    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    // 深夜勤務の可否
    public bool NightWorkAllowed { get; set; }

    // 週・月の最大勤務時間
    public decimal? WeeklyMaxHours { get; set; }

    public decimal? MonthlyMaxHours { get; set; }

    // 週・月の目標勤務時間
    public decimal? WeeklyTargetHours { get; set; }

    public decimal? MonthlyTargetHours { get; set; }

    // 登録・更新日時
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
