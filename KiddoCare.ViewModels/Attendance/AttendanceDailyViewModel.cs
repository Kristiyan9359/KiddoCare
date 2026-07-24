namespace KiddoCare.ViewModels.Attendance;

public class AttendanceDailyViewModel
{
    public DateTime Date { get; set; } = DateTime.Today;

    public int? GroupId { get; set; }

    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Groups { get; set; }
        = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

    public IEnumerable<AttendanceChildViewModel> Children { get; set; }
        = new List<AttendanceChildViewModel>();
}