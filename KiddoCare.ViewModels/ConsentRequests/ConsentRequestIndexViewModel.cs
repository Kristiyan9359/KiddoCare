using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.ConsentRequests;

public class ConsentRequestIndexViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public ConsentRequestType Type { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool CanRespond { get; set; }
}