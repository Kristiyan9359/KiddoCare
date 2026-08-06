using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class ChildDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [Required]
    public ChildDocumentType Type { get; set; }

    [Required]
    [MaxLength(ChildDocumentTitleMaxLength)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(ChildDocumentFileUrlMaxLength)]
    public string FileUrl { get; set; } = null!;

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [Required]
    public string UploadedByUserId { get; set; } = null!;

    public string? ReviewedByUserId { get; set; }

    public DateTime UploadedOn { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedOn { get; set; }

    [MaxLength(ChildDocumentReviewNoteMaxLength)]
    public string? ReviewNote { get; set; }

    public bool IsDeleted { get; set; }
}