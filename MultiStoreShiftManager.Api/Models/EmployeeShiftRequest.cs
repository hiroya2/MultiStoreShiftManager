namespace MultiStoreShiftManager.Api.Models;

public class EmployeeShiftRequest
{
    public int Id { get; set; }

    // 対象従業員
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // 希望対象日
    public DateOnly RequestDate { get; set; }

    // PreferOff / Unavailable / Available
    public string RequestType { get; set; } = "PreferOff";

    // 希望休の優先度（1～3）
    public int? Priority { get; set; }

    // 備考
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}