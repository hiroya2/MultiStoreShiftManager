using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeWorkConditionEndRequest : IValidatableObject
{
    [Required]
    public DateOnly EffectiveEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (EffectiveEndDate == default)
        {
            yield return new ValidationResult(
                "適用終了日を指定してください。",
                new[] { nameof(EffectiveEndDate) });
        }
    }
}