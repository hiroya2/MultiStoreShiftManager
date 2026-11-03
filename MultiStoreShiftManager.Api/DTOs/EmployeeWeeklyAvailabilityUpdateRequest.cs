using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeWeeklyAvailabilityUpdateRequest : IValidatableObject
{
    // 月曜日=1 ～ 日曜日=7
    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    // Unavailable / PreferOff / PreferWork / Available
    [Required]
    public string AvailabilityType { get; set; } = "Available";

    [Required]
    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var allowedTypes = new[]
        {
            "Unavailable",
            "PreferOff",
            "PreferWork",
            "Available"
        };

        if (!allowedTypes.Contains(AvailabilityType))
        {
            yield return new ValidationResult(
                "AvailabilityType is invalid.",
                new[] { nameof(AvailabilityType) });
        }

        if (EffectiveEndDate.HasValue &&
            EffectiveEndDate.Value < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "EffectiveEndDate must be on or after EffectiveStartDate.",
                new[] { nameof(EffectiveEndDate) });
        }
    }
}