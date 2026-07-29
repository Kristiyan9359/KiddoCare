using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Parents;

public class ParentEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(ParentFullNameMaxLength)]
    public string FullName { get; set; } = null!;

    [MaxLength(ParentPhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }
}