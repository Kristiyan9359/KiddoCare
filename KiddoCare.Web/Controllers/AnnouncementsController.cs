namespace KiddoCare.Web.Controllers;

using KiddoCare.Common;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Announcements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class AnnouncementsController : Controller
{
    private readonly IAnnouncementService announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        this.announcementService = announcementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? returnUrl, int page = 1, int pageSize = 15)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        AnnouncementListViewModel model = await this.announcementService
            .GetAllAsync(userId, isAdmin, isTeacher, searchTerm, page, pageSize);
        model.ReturnUrl = this.GetSafeReturnUrl(returnUrl);

        return this.View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        IEnumerable<string> suggestions = await this.announcementService
            .GetSearchSuggestionsAsync(term, userId, isAdmin, isTeacher);

        return this.Json(suggestions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        bool hasAccess = await this.announcementService
            .CanAccessAnnouncementAsync(id, userId, isAdmin, isTeacher);

        if (!hasAccess)
        {
            return this.NotFound();
        }

        AnnouncementDetailsViewModel? model =
            await this.announcementService.GetDetailsAsync(id);

        if (model == null)
        {
            return this.NotFound();
        }

        model.ReturnUrl = this.GetSafeReturnUrl(returnUrl);

        return this.View(model);
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        AnnouncementCreateViewModel model = await this.announcementService
            .GetCreateModelAsync(userId, isAdmin, isTeacher);

        return this.View(model);
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        if (!this.ModelState.IsValid)
        {
            AnnouncementCreateViewModel formModel = await this.announcementService
                .GetCreateModelAsync(userId, isAdmin, isTeacher);

            formModel.Title = model.Title;
            formModel.Content = model.Content;
            formModel.GroupId = model.GroupId;
            formModel.IsPublic = model.IsPublic;

            return this.View(formModel);
        }

        await this.announcementService
            .CreateAsync(model, userId, isAdmin, isTeacher);

        return this.RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        AnnouncementEditViewModel? model = await this.announcementService
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
    public async Task<IActionResult> Edit(AnnouncementEditViewModel model)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        if (!this.ModelState.IsValid)
        {
            AnnouncementEditViewModel? formModel = await this.announcementService
                .GetForEditAsync(model.Id, userId, isAdmin, isTeacher);

            if (formModel == null)
            {
                return this.NotFound();
            }

            formModel.Title = model.Title;
            formModel.Content = model.Content;
            formModel.GroupId = model.GroupId;
            formModel.IsPublic = model.IsPublic;

            return this.View(formModel);
        }

        await this.announcementService
            .EditAsync(model, userId, isAdmin, isTeacher);

        return this.RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Teacher}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        string userId = this.GetUserId();
        bool isAdmin = this.User.IsInRole(RoleConstants.Admin);
        bool isTeacher = this.User.IsInRole(RoleConstants.Teacher);

        await this.announcementService
            .DeleteAsync(id, userId, isAdmin, isTeacher);

        return this.RedirectToAction(nameof(Index));
    }

    private string GetUserId()
    {
        return this.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && this.Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }
}
