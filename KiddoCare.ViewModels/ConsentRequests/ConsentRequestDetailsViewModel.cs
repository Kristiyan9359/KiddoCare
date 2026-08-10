using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.ConsentRequests;

public class ConsentRequestDetailsViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public ConsentRequestType Type { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public string CreatedByEmail { get; set; } = null!;

    public string? ParentNote { get; set; }

    public DateTime? RespondedOn { get; set; }

    public bool CanRespond { get; set; }

    public string? ReturnUrl { get; set; }
}
