using System.ComponentModel.DataAnnotations;

namespace KiddoCare.Common.ValidationAttributes;

public class ChildBirthDateAttribute : ValidationAttribute
{
    private const int MaxChildAgeYears = 7;

    public override bool IsValid(object? value)
    {
        if (value is not DateTime dateOfBirth)
        {
            return false;
        }

        var today = DateTime.Today;
        var minDate = today.AddYears(-MaxChildAgeYears);

        return dateOfBirth >= minDate && dateOfBirth <= today;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be within the last {MaxChildAgeYears} years and cannot be in the future.";
    }
}