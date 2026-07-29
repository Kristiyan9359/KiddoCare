using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly IEventService eventService;

    public EventsController(IEventService eventService)
    {
        this.eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isAdminOrTeacher = isAdmin || isTeacher;

        var events = await eventService.GetAllAsync(userId, isAdmin, isTeacher);

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isAdminOrTeacher = isAdmin || isTeacher;

        var canAccess = await eventService.CanAccessEventAsync(id, userId, isAdminOrTeacher);

        if (!canAccess)
        {
            return Forbid();
        }

        var model = await eventService.GetDetailsAsync(id);

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

        var model = await eventService.GetCreateModelAsync(userId, isAdmin, isTeacher);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Create(EventCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await eventService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Groups = createModel.Groups;

            return View(model);
        }

        try
        {
            await eventService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await eventService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Groups = createModel.Groups;

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

        var model = await eventService.GetForEditAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Edit(EventEditViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var editModel = await eventService.GetForEditAsync(model.Id, userId, isAdmin, isTeacher);

            if (editModel == null)
            {
                return NotFound();
            }

            model.Groups = editModel.Groups;

            return View(model);
        }

        try
        {
            await eventService.EditAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        try
        {
            await eventService.DeleteAsync(id, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}