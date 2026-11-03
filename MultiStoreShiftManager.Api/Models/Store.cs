
using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.Models;

public class Store
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string StoreCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string StoreName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}
