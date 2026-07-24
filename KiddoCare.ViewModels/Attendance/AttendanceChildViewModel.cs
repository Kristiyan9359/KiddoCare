using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceChildViewModel
{
    public int ChildId { get; set; }

    public string FullName { get; set; } = null!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public string? Note { get; set; }
}