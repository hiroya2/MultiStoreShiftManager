using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeAvailableTimeUpdateRequest : IValidatableObject
{
    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool EndsNextDay { get; set; }

    [Required]
    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (EffectiveEndDate.HasValue &&
            EffectiveEndDate.Value < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "適用終了日は適用開始日より前にできません。",
                new[] { nameof(EffectiveEndDate) });
        }

        if (StartTime == EndTime)
        {
            yield return new ValidationResult(
                "開始時刻と終了時刻は異なる時刻にしてください。",
                new[] { nameof(EndTime) });
        }

        if (!EndsNextDay && EndTime < StartTime)
        {
            yield return new ValidationResult(
                "終了時刻が開始時刻より前の場合は、日付またぎを有効にしてください。",
                new[] { nameof(EndsNextDay) });
        }
    }
}