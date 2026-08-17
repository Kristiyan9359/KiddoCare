using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.ChildDocuments;

public class ChildDocumentReviewViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public ChildDocumentType Type { get; set; }

    public string FileUrl { get; set; } = null!;

    [Display(Name = "Review Decision")]
    [Range(1, 2, ErrorMessage = "Please select a review decision.")]
    public RequestStatus Status { get; set; }

    [Display(Name = "Review Note")]
    [MaxLength(ChildDocumentReviewNoteMaxLength, ErrorMessage = "Review note cannot be longer than {1} characters.")]
    public string? ReviewNote { get; set; }

    public string? ReturnUrl { get; set; }
}
