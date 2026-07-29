using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Parents;

public class ParentCreateViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(ParentFullNameMaxLength)]
    public string FullName { get; set; } = null!;

    [MaxLength(ParentPhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }
}