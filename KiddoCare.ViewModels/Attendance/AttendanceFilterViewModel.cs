using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceFilterViewModel
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int? GroupId { get; set; }

    public AttendanceStatus? Status { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();

    public IEnumerable<AttendanceRecordViewModel> Records { get; set; } = new List<AttendanceRecordViewModel>();
}