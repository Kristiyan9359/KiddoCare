using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class ConsentRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [Required]
    [MaxLength(ConsentRequestTitleMaxLength)]
    public string Title { get; set; } = null!;

    [MaxLength(ConsentRequestDescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    public ConsentRequestType Type { get; set; }

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [Required]
    public string CreatedByUserId { get; set; } = null!;

    public string? RespondedByUserId { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedOn { get; set; }

    [MaxLength(ConsentRequestParentNoteMaxLength)]
    public string? ParentNote { get; set; }

    public bool IsDeleted { get; set; }
}