using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.ChildDocuments;

public class ChildDocumentIndexViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public ChildDocumentType Type { get; set; }

    public string Title { get; set; } = null!;

    public RequestStatus Status { get; set; }

    public DateTime UploadedOn { get; set; }

    public bool CanReview { get; set; }
}