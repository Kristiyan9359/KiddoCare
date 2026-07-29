using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class Announcement
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(AnnouncementTitleMaxLength)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(AnnouncementContentMaxLength)]
    public string Content { get; set; } = null!;

    [ForeignKey(nameof(Group))]
    public int? GroupId { get; set; }

    public KindergartenGroup? Group { get; set; }

    public bool IsPublic { get; set; } = true;

    public DateTime PublishedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
}