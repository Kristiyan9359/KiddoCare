using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Children;

public class ChildDetailsConsentRequestViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public ConsentRequestType Type { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime CreatedOn { get; set; }
}