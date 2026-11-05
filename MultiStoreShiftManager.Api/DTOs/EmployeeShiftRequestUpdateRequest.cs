using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeShiftRequestUpdateRequest
{
    [Required]
    [RegularExpression("^(PreferOff|Unavailable|Available)$")]
    public string RequestType { get; set; } = "PreferOff";

    [Range(1, 3)]
    public int? Priority { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}