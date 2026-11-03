using System.ComponentModel.DataAnnotations;

namespace MultiStoreShiftManager.Api.DTOs;

public class EmployeeStoreAssignmentCreateRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(1, int.MaxValue)]
    public int StoreId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [CustomValidation(
    typeof(EmployeeStoreAssignmentCreateRequest),
    nameof(ValidateEndDate))]
    public DateOnly? EndDate { get; set; }

    public static ValidationResult? ValidateEndDate(
    DateOnly? endDate,
    ValidationContext context)
    {
        var instance =
            (EmployeeStoreAssignmentCreateRequest)context.ObjectInstance;

        if (endDate.HasValue && endDate.Value < instance.StartDate)
        {
            return new ValidationResult(
                "勤務可能終了日は開始日より後の日付にしてください。");
        }

        return ValidationResult.Success;
    }
}