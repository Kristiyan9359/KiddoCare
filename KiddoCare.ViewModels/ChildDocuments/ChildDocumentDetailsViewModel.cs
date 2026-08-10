using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.ChildDocuments;

public class ChildDocumentDetailsViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public ChildDocumentType Type { get; set; }

    public string Title { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public RequestStatus Status { get; set; }

    public string UploadedByEmail { get; set; } = null!;

    public DateTime UploadedOn { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? ReviewedOn { get; set; }

    public bool CanReview { get; set; }

    public string? ReturnUrl { get; set; }
}
