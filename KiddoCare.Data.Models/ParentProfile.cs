using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class ParentProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public IdentityUser User { get; set; } = null!;

    [Required]
    [MaxLength(ParentFullNameMaxLength)]
    public string FullName { get; set; } = null!;

    [MaxLength(ParentPhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<Child> Children { get; set; } = new HashSet<Child>();
}