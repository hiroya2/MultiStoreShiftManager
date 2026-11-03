using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeAvailableTimeEndRequest
{
    [Required]
    public DateOnly EffectiveEndDate { get; set; }
}