using System.Security.Claims;
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
    public async Task<IActionResult> Index(string? medicalFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var children = await childService.GetAllAsync(userId, isAdmin, isTeacher, medicalFilter);

        ViewBag.MedicalFilter = medicalFilter;

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
            var createModel = await childService.GetCreateModelAsync();
            model.Groups = createModel.Groups;
            model.Parents = createModel.Parents;
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
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await childService.GetForDeleteAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
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

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var canAccess = await childService.CanAccessChildAsync(id, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return Forbid();
        }

        var model = await childService.GetDetailsAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }
}