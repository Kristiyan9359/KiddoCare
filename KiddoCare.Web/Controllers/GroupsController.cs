using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize(Roles = Admin)]
public class GroupsController : Controller
{
    private readonly IGroupService groupService;

    public GroupsController(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var groups = await groupService.GetAllAsync();

        return View(groups);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(GroupCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await groupService.CreateAsync(model);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await groupService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(GroupEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await groupService.EditAsync(model);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await groupService.DeleteAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}