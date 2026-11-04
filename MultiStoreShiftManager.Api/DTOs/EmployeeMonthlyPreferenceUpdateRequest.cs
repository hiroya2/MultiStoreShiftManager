using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeMonthlyPreferenceUpdateRequest
{
    [Required]
    [RegularExpression("^(PreferOff|Unavailable|Available)$")]
    public string AvailabilityType { get; set; } = "Available";
}