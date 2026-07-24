using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceChildViewModel
{
    public int ChildId { get; set; }

    [ValidateNever]
    public string FullName { get; set; } = null!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public string? Note { get; set; }
}