using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Groups;

public class GroupCreateViewModel
{
    [Required]
    [MaxLength(KindergartenGroupNameMaxLength)]
    public string Name { get; set; } = null!;

    [MaxLength(KindergartenGroupDescriptionMaxLength)]
    public string? Description { get; set; }
}