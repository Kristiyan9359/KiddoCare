using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.AbsenceRequests;

public class AbsenceRequestIndexViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AbsenceReason Reason { get; set; }

    public AbsenceRequestStatus Status { get; set; }

    public bool CanReview { get; set; }
}