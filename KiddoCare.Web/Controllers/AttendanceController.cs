using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await attendanceService.GetDailyAttendanceAsync(
            selectedDate,
            groupId,
            userId,
            isAdmin,
            isTeacher);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Daily(AttendanceDailyViewModel model)
    {
        if (model.Children.Count == 0)
        {
            var userIdForReload = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdminForReload = User.IsInRole(Admin);
            var isTeacherForReload = User.IsInRole(Teacher);

            model = await attendanceService.GetDailyAttendanceAsync(
                model.Date,
                model.GroupId,
                userIdForReload,
                isAdminForReload,
                isTeacherForReload);

            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        await attendanceService.SaveDailyAttendanceAsync(
            model,
            userId,
            isAdmin,
            isTeacher);

        return RedirectToAction(nameof(Daily), new
        {
            date = model.Date.ToString("yyyy-MM-dd"),
            groupId = model.GroupId
        });
    }

    [HttpGet]
    public async Task<IActionResult> History(AttendanceFilterViewModel filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await attendanceService.GetHistoryAsync(
            filter,
            userId,
            isAdmin,
            isTeacher);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await attendanceService.GetForEditAsync(
            id,
            userId,
            isAdmin,
            isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(AttendanceEditViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var invalidModel = await attendanceService.GetForEditAsync(
                model.Id,
                userId,
                isAdmin,
                isTeacher);

            if (invalidModel == null)
            {
                return NotFound();
            }

            invalidModel.Status = model.Status;
            invalidModel.Note = model.Note;
            invalidModel.ReturnUrl = model.ReturnUrl;

            return View(invalidModel);
        }

        try
        {
            await attendanceService.EditAsync(
                model,
                userId,
                isAdmin,
                isTeacher);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToLocalOrHistory(model.ReturnUrl);
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        return Url.IsLocalUrl(returnUrl) ? returnUrl : null;
    }

    private IActionResult RedirectToLocalOrHistory(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(History));
    }
}