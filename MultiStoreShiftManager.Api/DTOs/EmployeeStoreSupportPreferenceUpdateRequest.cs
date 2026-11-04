using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeStoreSupportPreferenceUpdateRequest
{
    [Required]
    public bool IsAvailable { get; set; }  //フォロー可能かどうか

    [Range(1, 100)]
    public int? Priority { get; set; }  //フォロー優先順位
}