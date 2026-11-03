namespace MultiStoreShiftManager.Api.Models;

public class EmployeeWeeklyAvailability
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    // 月曜日=1 ～ 日曜日=7
    public int DayOfWeek { get; set; }

    // Unavailable / PreferOff / PreferWork / Available
    public string AvailabilityType { get; set; } = "Available";

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}