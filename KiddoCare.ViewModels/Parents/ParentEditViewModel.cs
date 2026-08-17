using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Parents;

public class ParentEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Full Name")]
    [Required(ErrorMessage = "Please enter the parent's full name.")]
    [MaxLength(ParentFullNameMaxLength, ErrorMessage = "Full name cannot be longer than {1} characters.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Phone number")]
    [MaxLength(ParentPhoneNumberMaxLength, ErrorMessage = "Phone number cannot be longer than {1} characters.")]
    public string? PhoneNumber { get; set; }
}
