using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Children;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ChildrenController : Controller
{
    private readonly IChildService childService;

    public ChildrenController(IChildService childService)
    {
        this.childService = childService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var children = await childService.GetAllAsync();

        return View(children);
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await childService.GetCreateModelAsync();

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(ChildCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Groups = (await childService.GetCreateModelAsync()).Groups;
            return View(model);
        }

        await childService.CreateAsync(model);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await childService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Edit(ChildEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var editModel = await childService.GetForEditAsync(model.Id);

            if (editModel == null)
            {
                return NotFound();
            }

            model.Groups = editModel.Groups;

            return View(model);
        }

        try
        {
            await childService.EditAsync(model);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await childService.DeleteAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await childService.GetDetailsAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }
}