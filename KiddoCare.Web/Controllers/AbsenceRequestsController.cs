using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.AbsenceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class AbsenceRequestsController : Controller
{
    private readonly IAbsenceRequestService absenceRequestService;

    public AbsenceRequestsController(IAbsenceRequestService absenceRequestService)
    {
        this.absenceRequestService = absenceRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? statusFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await absenceRequestService.GetAllAsync(userId, isAdmin, isTeacher, statusFilter);

        ViewBag.StatusFilter = statusFilter;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await absenceRequestService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await absenceRequestService.GetCreateModelAsync(userId, isAdmin, isTeacher);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AbsenceRequestCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await absenceRequestService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            await absenceRequestService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await absenceRequestService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Review(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await absenceRequestService.GetForReviewAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Review(AbsenceRequestReviewViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        ModelState.Remove(nameof(AbsenceRequestReviewViewModel.ChildFullName));

        if (!ModelState.IsValid)
        {
            var reviewModel = await absenceRequestService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.ReviewNote = model.ReviewNote;
            reviewModel.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(reviewModel);
        }

        try
        {
            await absenceRequestService.ReviewAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var reviewModel = await absenceRequestService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.ReviewNote = model.ReviewNote;
            reviewModel.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(reviewModel);
        }

        return RedirectToLocalOrIndex(model.ReturnUrl);
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
