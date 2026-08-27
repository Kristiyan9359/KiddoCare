using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ChildDocuments;
using KiddoCare.Web.Extensions;
using KiddoCare.Web.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ChildDocumentsController : Controller
{
    private readonly IChildDocumentService childDocumentService;
    private readonly IFileStorageService fileStorageService;
    private readonly IStringLocalizer<SharedResource> localizer;

    public ChildDocumentsController(IChildDocumentService childDocumentService, IFileStorageService fileStorageService, IStringLocalizer<SharedResource> localizer)
    {
        this.childDocumentService = childDocumentService;
        this.fileStorageService = fileStorageService;
        this.localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? returnUrl, int page = 1, int pageSize = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetAllAsync(userId, isAdmin, isTeacher, searchTerm, statusFilter, page, pageSize);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var suggestions = await childDocumentService.GetSearchSuggestionsAsync(term, userId, isAdmin, isTeacher);

        return Json(suggestions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        var storedFile = fileStorageService.GetChildDocument(model.FileUrl, model.Title);

        if (storedFile == null)
        {
            return NotFound();
        }

        return PhysicalFile(storedFile.FilePath!, storedFile.ContentType, storedFile.DownloadName);
    }

    [Authorize(Roles = $"{Admin},{Parent}")]
    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Parent}")]
    [HttpPost]
    public async Task<IActionResult> Create(ChildDocumentCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            model.FileUrl = await fileStorageService.SaveChildDocumentAsync(model.File);

            await childDocumentService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);

            var createModel = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        this.SetSuccessMessage("Child document uploaded successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Review(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetForReviewAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Review(ChildDocumentReviewViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        ModelState.Remove(nameof(ChildDocumentReviewViewModel.ChildFullName));
        ModelState.Remove(nameof(ChildDocumentReviewViewModel.Title));
        ModelState.Remove(nameof(ChildDocumentReviewViewModel.FileUrl));

        if (!ModelState.IsValid)
        {
            var reviewModel = await childDocumentService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.Status = model.Status;
            reviewModel.ReviewNote = model.ReviewNote;
            reviewModel.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(reviewModel);
        }

        try
        {
            await childDocumentService.ReviewAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);

            var reviewModel = await childDocumentService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.Status = model.Status;
            reviewModel.ReviewNote = model.ReviewNote;
            reviewModel.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(reviewModel);
        }

        this.SetSuccessMessage("Child document review saved successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
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
