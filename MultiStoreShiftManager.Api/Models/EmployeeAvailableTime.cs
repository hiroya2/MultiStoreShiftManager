namespace MultiStoreShiftManager.Api.Models;

public class EmployeeAvailableTime
{
    public int Id { get; set; }

    // 対象従業員
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // 曜日（月曜日=1 ～ 日曜日=7）
    public int DayOfWeek { get; set; }

    // 勤務可能時間
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // 日付をまたぐ勤務可能時間か
    public bool EndsNextDay { get; set; }

    // 適用期間
    public DateOnly EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}