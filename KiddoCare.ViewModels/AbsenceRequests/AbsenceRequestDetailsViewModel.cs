using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.AbsenceRequests;

public class AbsenceRequestDetailsViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AbsenceReason Reason { get; set; }

    public string? ParentNote { get; set; }

    public AbsenceRequestStatus Status { get; set; }

    public string RequestedByEmail { get; set; } = null!;

    public DateTime RequestedOn { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime? ReviewedOn { get; set; }

    public bool CanReview { get; set; }
}