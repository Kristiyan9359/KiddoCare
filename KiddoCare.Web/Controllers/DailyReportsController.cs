using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.DailyReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class DailyReportsController : Controller
{
    private readonly IDailyReportService dailyReportService;

    public DailyReportsController(IDailyReportService dailyReportService)
    {
        this.dailyReportService = dailyReportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var reports = await dailyReportService.GetAllAsync(userId, isAdmin, isTeacher);

        return View(reports);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var canAccess = await dailyReportService.CanAccessAsync(id, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return Forbid();
        }

        var model = await dailyReportService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetCreateModelAsync(userId, isAdmin, isTeacher);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Create(DailyReportCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await dailyReportService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        try
        {
            await dailyReportService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await dailyReportService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetForEditAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Edit(DailyReportEditViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await dailyReportService.EditAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetForDeleteAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        try
        {
            await dailyReportService.DeleteAsync(id, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}