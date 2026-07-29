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
        var isAdminOrTeacher = User.IsInRole(Admin) || User.IsInRole(Teacher);

        var events = await eventService.GetAllAsync(userId, isAdminOrTeacher);

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdminOrTeacher = User.IsInRole(Admin) || User.IsInRole(Teacher);

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
        var model = await eventService.GetCreateModelAsync();

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpPost]
    public async Task<IActionResult> Create(EventCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Groups = (await eventService.GetCreateModelAsync()).Groups;
            return View(model);
        }

        await eventService.CreateAsync(model);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Admin},{Teacher}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await eventService.GetForEditAsync(id);

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
        if (!ModelState.IsValid)
        {
            var editModel = await eventService.GetForEditAsync(model.Id);

            if (editModel == null)
            {
                return NotFound();
            }

            model.Groups = editModel.Groups;
            return View(model);
        }

        try
        {
            await eventService.EditAsync(model);
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
        try
        {
            await eventService.DeleteAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}