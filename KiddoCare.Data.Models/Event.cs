using KiddoCare.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class Event
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(EventTitleMaxLength)]
    public string Title { get; set; } = null!;

    [MaxLength(EventDescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    [Required]
    public EventType Type { get; set; } = EventType.General;

    [MaxLength(EventLocationMaxLength)]
    public string? Location { get; set; }

    [ForeignKey(nameof(Group))]
    public int? GroupId { get; set; }

    public KindergartenGroup? Group { get; set; }

    public bool IsPublic { get; set; } = true;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
}