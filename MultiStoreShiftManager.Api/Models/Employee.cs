using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.Models;

public class Employee
{
    public int Id { get; set; }

    // 従業員固有の管理番号
    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    // 氏名
    [Required]
    [MaxLength(100)]
    public string EmployeeName { get; set; } = string.Empty;

    // 雇用区分
    [Required]
    [MaxLength(20)]
    public string EmploymentType { get; set; } = string.Empty;

    // 在籍状態
    [Required]
    [MaxLength(20)]
    public string EmploymentStatus { get; set; } = "Active";

    // 入社日
    public DateOnly? HireDate { get; set; }

    // 退職日
    public DateOnly? RetirementDate { get; set; }

    // 有効・無効
    public bool IsActive { get; set; } = true;

    // 登録日時
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    // 更新日時
    public DateTimeOffset? UpdatedAt { get; set; }
}