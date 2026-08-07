using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Children;

public class ChildDetailsDocumentViewModel
{
    public int Id { get; set; }

    public ChildDocumentType Type { get; set; }

    public string Title { get; set; } = null!;

    public RequestStatus Status { get; set; }

    public DateTime UploadedOn { get; set; }
}