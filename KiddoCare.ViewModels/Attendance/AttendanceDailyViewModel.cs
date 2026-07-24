using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceDailyViewModel
{
    public DateTime Date { get; set; } = DateTime.Today;

    public int? GroupId { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();

    public List<AttendanceChildViewModel> Children { get; set; } = new();
}