using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeUpdateRequest
{
    // 従業員管理番号
    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    // 氏名
    [Required]
    [MaxLength(100)]
    public string EmployeeName { get; set; } = string.Empty;

    // 雇用区分
    [Required]
    [RegularExpression("^(FullTime|PartTime|ShortTime)$",
        ErrorMessage = "雇用区分が正しくありません。")]
    public string EmploymentType { get; set; } = string.Empty;

    // 入社日
    public DateOnly? HireDate { get; set; }
}