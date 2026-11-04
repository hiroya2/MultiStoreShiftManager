namespace MultiStoreShiftManager.Api.Models;

public class EmployeeStoreSupportPreference
{
    public int Id { get; set; }

    // フォローする従業員
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // フォロー先の店舗
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    // フォロー可能かどうか
    public bool IsAvailable { get; set; } = true;

    // フォロー優先度（数字が小さいほど優先）
    public int? Priority { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}