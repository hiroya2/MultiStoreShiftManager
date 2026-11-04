using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeMonthlyPreferenceCreateRequest
{
    [Required]
    public int EmployeeId { get; set; }

    // 対象年月（毎月1日）
    [Required]
    public DateOnly TargetMonth { get; set; }

    // 月曜日=1 ～ 日曜日=7
    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    // PreferOff / Unavailable / Available
    [Required]
    [RegularExpression("^(PreferOff|Unavailable|Available)$")]
    public string AvailabilityType { get; set; } = "Available";
}