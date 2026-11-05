using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeShiftRequestCreateRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateOnly RequestDate { get; set; }

    [Required]
    [RegularExpression("^(PreferOff|Unavailable|Available)$")]
    public string RequestType { get; set; } = "PreferOff";

    [Range(1, 3)]
    public int? Priority { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}