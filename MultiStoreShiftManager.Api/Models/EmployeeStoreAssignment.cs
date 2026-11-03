using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.Models;

public class EmployeeStoreAssignment
{
    public int Id { get; set; }

    // 従業員ID
    public int EmployeeId { get; set; }

    // 店舗ID
    public int StoreId { get; set; }

    // 勤務可能開始日
    public DateOnly StartDate { get; set; }

    // 勤務可能終了日
    public DateOnly? EndDate { get; set; }

    // 登録日時
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    // 更新日時
    public DateTimeOffset? UpdatedAt { get; set; }

    // 関連する従業員
    public Employee Employee { get; set; } = null!;

    // 関連する店舗
    public Store Store { get; set; } = null!;
}