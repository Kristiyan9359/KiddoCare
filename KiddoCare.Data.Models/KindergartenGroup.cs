using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class KindergartenGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(KindergartenGroupNameMaxLength)]
    public string Name { get; set; } = null!;

    [MaxLength(KindergartenGroupDescriptionMaxLength)]
    public string? Description { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public ICollection<Child> Children { get; set; } = new HashSet<Child>();

    public ICollection<Event> Events { get; set; } = new HashSet<Event>();

    public ICollection<TeacherProfile> Teachers { get; set; } = new HashSet<TeacherProfile>();
}