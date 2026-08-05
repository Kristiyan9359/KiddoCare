using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ConsentRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ConsentRequestsController : Controller
{
    private readonly IConsentRequestService consentRequestService;

    public ConsentRequestsController(IConsentRequestService consentRequestService)
    {
        this.consentRequestService = consentRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? statusFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await consentRequestService.GetAllAsync(userId, isAdmin, isTeacher, statusFilter);

        ViewBag.StatusFilter = statusFilter;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await consentRequestService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

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

        var model = await consentRequestService.GetCreateModelAsync(userId, isAdmin, isTeacher);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Create(ConsentRequestCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await consentRequestService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        try
        {
            await consentRequestService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await consentRequestService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Respond(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var model = await consentRequestService.GetForRespondAsync(id, userId);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Respond(ConsentRequestRespondViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        ModelState.Remove(nameof(ConsentRequestRespondViewModel.ChildFullName));
        ModelState.Remove(nameof(ConsentRequestRespondViewModel.Title));

        if (!ModelState.IsValid)
        {
            var respondModel = await consentRequestService.GetForRespondAsync(model.Id, userId);

            if (respondModel == null)
            {
                return NotFound();
            }

            respondModel.Status = model.Status;
            respondModel.ParentNote = model.ParentNote;

            return View(respondModel);
        }

        try
        {
            await consentRequestService.RespondAsync(model, userId);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var respondModel = await consentRequestService.GetForRespondAsync(model.Id, userId);

            if (respondModel == null)
            {
                return NotFound();
            }

            respondModel.Status = model.Status;
            respondModel.ParentNote = model.ParentNote;

            return View(respondModel);
        }

        return RedirectToAction(nameof(Index));
    }
}