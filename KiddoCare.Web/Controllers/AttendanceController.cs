using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize(Roles = $"{Admin},{Teacher}")]
public class AttendanceController : Controller
{
    private readonly IAttendanceService attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        this.attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Daily(DateTime? date, int? groupId)
    {
        var selectedDate = date ?? DateTime.Today;

        var model = await attendanceService.GetDailyAttendanceAsync(selectedDate, groupId);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Daily(AttendanceDailyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await attendanceService.GetDailyAttendanceAsync(model.Date, model.GroupId);
            return View(model);
        }

        await attendanceService.SaveDailyAttendanceAsync(model);

        return RedirectToAction(nameof(Daily), new
        {
            date = model.Date.ToString("yyyy-MM-dd"),
            groupId = model.GroupId
        });
    }
}