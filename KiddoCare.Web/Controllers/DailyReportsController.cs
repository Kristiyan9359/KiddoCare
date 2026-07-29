namespace KiddoCare.Web.Controllers;

using KiddoCare.Common;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.DailyReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        var reports = await this.dailyReportService
            .GetAllAsync(userId, isAdmin, isTeacher);

        return this.View(reports);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        bool hasAccess = await this.dailyReportService
            .CanAccessAsync(id, userId, isAdmin, isTeacher);

        if (!hasAccess)
        {
            return this.NotFound();
        }

        DailyReportDetailsViewModel? model = await this.dailyReportService
            .GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return this.NotFound();
        }

        return this.View(model);
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        DailyReportCreateViewModel model = await this.dailyReportService
            .GetCreateModelAsync(userId, isAdmin, isTeacher);

        return this.View(model);
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DailyReportCreateViewModel model)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        if (!this.ModelState.IsValid)
        {
            return this.View(await this.GetCreateModelWithInputAsync(
                model,
                userId,
                isAdmin,
                isTeacher));
        }

        try
        {
            await this.dailyReportService
                .CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException exception)
        {
            this.ModelState.AddModelError(string.Empty, exception.Message);

            return this.View(await this.GetCreateModelWithInputAsync(
                model,
                userId,
                isAdmin,
                isTeacher));
        }

        return this.RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        DailyReportEditViewModel? model = await this.dailyReportService
            .GetForEditAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return this.NotFound();
        }

        return this.View(model);
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DailyReportEditViewModel model)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        try
        {
            await this.dailyReportService
                .EditAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException exception)
        {
            this.ModelState.AddModelError(string.Empty, exception.Message);

            return this.View(model);
        }

        return this.RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        DailyReportDeleteViewModel? model = await this.dailyReportService
            .GetForDeleteAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return this.NotFound();
        }

        return this.View(model);
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        try
        {
            await this.dailyReportService
                .DeleteAsync(id, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException)
        {
            return this.NotFound();
        }

        return this.RedirectToAction(nameof(Index));
    }

    private async Task<DailyReportCreateViewModel> GetCreateModelWithInputAsync(
        DailyReportCreateViewModel model,
        string userId,
        bool isAdmin,
        bool isTeacher)
    {
        DailyReportCreateViewModel formModel = await this.dailyReportService
            .GetCreateModelAsync(userId, isAdmin, isTeacher);

        formModel.ChildId = model.ChildId;
        formModel.ReportDate = model.ReportDate;
        formModel.Mood = model.Mood;
        formModel.Meals = model.Meals;
        formModel.Sleep = model.Sleep;
        formModel.Activities = model.Activities;
        formModel.TeacherNote = model.TeacherNote;

        return formModel;
    }

    private string GetUserId()
    {
        return this.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}