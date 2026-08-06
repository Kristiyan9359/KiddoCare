using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ChildDocuments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ChildDocumentsController : Controller
{
    private readonly IChildDocumentService childDocumentService;

    public ChildDocumentsController(IChildDocumentService childDocumentService)
    {
        this.childDocumentService = childDocumentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? statusFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetAllAsync(userId, isAdmin, isTeacher, statusFilter);

        ViewBag.StatusFilter = statusFilter;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);

        return View(model);
    }

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

            return View(model);
        }

        try
        {
            await childDocumentService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetForReviewAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

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

            return View(reviewModel);
        }

        try
        {
            await childDocumentService.ReviewAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var reviewModel = await childDocumentService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.Status = model.Status;
            reviewModel.ReviewNote = model.ReviewNote;

            return View(reviewModel);
        }

        return RedirectToAction(nameof(Index));
    }
}