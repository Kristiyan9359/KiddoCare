using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Groups;

public class GroupEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Group name")]
    [Required(ErrorMessage = "Please enter a group name.")]
    [MaxLength(KindergartenGroupNameMaxLength, ErrorMessage = "Group name cannot be longer than {1} characters.")]
    public string Name { get; set; } = null!;

    [Display(Name = "Description")]
    [MaxLength(KindergartenGroupDescriptionMaxLength, ErrorMessage = "Description cannot be longer than {1} characters.")]
    public string? Description { get; set; }
}