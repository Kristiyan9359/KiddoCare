using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceRecordViewModel
{
    public DateTime Date { get; set; }

    public string ChildName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }
}