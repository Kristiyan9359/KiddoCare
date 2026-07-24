namespace KiddoCare.ViewModels.Attendance;

public class AttendanceSummaryViewModel
{
    public int PresentCount { get; set; }

    public int AbsentCount { get; set; }

    public int SickCount { get; set; }

    public int VacationCount { get; set; }

    public int LateCount { get; set; }

    public int TotalCount { get; set; }
}