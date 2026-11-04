namespace MultiStoreShiftManager.Api.Models;

public class EmployeeMonthlyPreference
{
    public int Id { get; set; }

    // 対象従業員
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // 対象年月（毎月1日を保存）
    public DateOnly TargetMonth { get; set; }

    // 月曜日=1 ～ 日曜日=7
    public int DayOfWeek { get; set; }

    // PreferOff / Unavailable / Available
    public string AvailabilityType { get; set; } = "Available";

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}