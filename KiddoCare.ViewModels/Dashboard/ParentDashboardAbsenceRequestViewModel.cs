using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Dashboard;

public class ParentDashboardAbsenceRequestViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AbsenceReason Reason { get; set; }

    public RequestStatus Status { get; set; }
}