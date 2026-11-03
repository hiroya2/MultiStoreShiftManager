using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeWorkConditionUpdateRequest : IValidatableObject
{
    [Required]
    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public bool NightWorkAllowed { get; set; }

    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal? WeeklyMaxHours { get; set; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? MonthlyMaxHours { get; set; }

    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal? WeeklyTargetHours { get; set; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? MonthlyTargetHours { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (EffectiveEndDate.HasValue &&
            EffectiveEndDate.Value < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "適用終了日は適用開始日より後の日付にしてください。",
                new[] { nameof(EffectiveEndDate) });
        }
    }
}