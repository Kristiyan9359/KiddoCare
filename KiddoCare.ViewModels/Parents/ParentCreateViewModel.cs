using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Parents;

public class ParentCreateViewModel
{
    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Please enter the parent's email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Display(Name = "Full name")]
    [Required(ErrorMessage = "Please enter the parent's full name.")]
    [MaxLength(ParentFullNameMaxLength, ErrorMessage = "Full name cannot be longer than {1} characters.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Phone number")]
    [MaxLength(ParentPhoneNumberMaxLength, ErrorMessage = "Phone number cannot be longer than {1} characters.")]
    public string? PhoneNumber { get; set; }
}