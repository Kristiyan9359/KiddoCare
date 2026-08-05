using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class AbsenceRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public AbsenceReason Reason { get; set; }

    [MaxLength(AbsenceRequestParentNoteMaxLength)]
    public string? ParentNote { get; set; }

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [Required]
    public string RequestedByUserId { get; set; } = null!;

    public string? ReviewedByUserId { get; set; }

    public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedOn { get; set; }

    [MaxLength(AbsenceRequestReviewNoteMaxLength)]
    public string? ReviewNote { get; set; }

    public bool IsDeleted { get; set; }
}