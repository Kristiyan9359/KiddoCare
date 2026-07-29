using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class TeacherProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; } = null!;

    [Required]
    [MaxLength(TeacherFullNameMaxLength)]
    public string FullName { get; set; } = null!;

    [MaxLength(TeacherPhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

    [Required]
    [ForeignKey(nameof(Group))]
    public int GroupId { get; set; }

    public KindergartenGroup Group { get; set; } = null!;

    public bool IsDeleted { get; set; }
}