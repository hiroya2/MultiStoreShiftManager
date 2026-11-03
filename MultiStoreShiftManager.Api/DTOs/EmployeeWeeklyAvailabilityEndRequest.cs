using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeWeeklyAvailabilityEndRequest
{
    [Required]
    public DateOnly EffectiveEndDate { get; set; }
}