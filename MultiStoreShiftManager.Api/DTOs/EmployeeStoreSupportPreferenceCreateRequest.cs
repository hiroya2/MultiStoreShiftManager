using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeStoreSupportPreferenceCreateRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int StoreId { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Range(1, 100)]
    public int? Priority { get; set; }
}