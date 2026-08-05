using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Children;

public class ChildDetailsAbsenceRequestViewModel
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AbsenceReason Reason { get; set; }

    public RequestStatus Status { get; set; }
}