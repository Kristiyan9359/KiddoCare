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
    public async Task<IActionResult> Index(string? searchTerm, string? returnUrl, int page = 1, int pageSize = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetAllAsync(userId, isAdmin, isTeacher, searchTerm, page, pageSize);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var suggestions = await dailyReportService.GetSearchSuggestionsAsync(term, userId, isAdmin, isTeacher);

        return Json(suggestions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
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

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetCreateModelAsync(userId, isAdmin, isTeacher);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

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
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

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
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetForEditAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

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
            var editModel = await dailyReportService.GetForEditAsync(model.Id, userId, isAdmin, isTeacher);

            if (editModel == null)
            {
                return NotFound();
            }

            model.ChildFullName = editModel.ChildFullName;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            await dailyReportService.EditAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var editModel = await dailyReportService.GetForEditAsync(model.Id, userId, isAdmin, isTeacher);

            if (editModel == null)
            {
                return NotFound();
            }

            model.ChildFullName = editModel.ChildFullName;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await dailyReportService.GetForDeleteAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl)
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

        return RedirectToLocalOrIndex(returnUrl);
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private IActionResult RedirectToLocalOrIndex(string? returnUrl)
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        if (safeReturnUrl != null)
        {
            return Redirect(safeReturnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
