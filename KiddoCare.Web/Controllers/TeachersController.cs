using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Teachers;
using KiddoCare.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize(Roles = Admin)]
public class TeachersController : Controller
{
    private readonly ITeacherService teacherService;
    private readonly IStringLocalizer<SharedResource> localizer;

    public TeachersController(ITeacherService teacherService, IStringLocalizer<SharedResource> localizer)
    {
        this.teacherService = teacherService;
        this.localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = 15)
    {
        var model = await teacherService.GetAllAsync(searchTerm, page, pageSize);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await teacherService.GetDetailsAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await teacherService.GetCreateModelAsync();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TeacherCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Groups = (await teacherService.GetCreateModelAsync()).Groups;
            return View(model);
        }

        try
        {
            await teacherService.CreateAsync(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);
            model.Groups = (await teacherService.GetCreateModelAsync()).Groups;
            return View(model);
        }

        this.SetSuccessMessage("Teacher created successfully.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await teacherService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TeacherEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var editModel = await teacherService.GetForEditAsync(model.Id);

            if (editModel == null)
            {
                return NotFound();
            }

            model.Groups = editModel.Groups;
            return View(model);
        }

        try
        {
            await teacherService.EditAsync(model);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        this.SetSuccessMessage("Teacher updated successfully.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await teacherService.GetForDeleteAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await teacherService.DeleteAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        this.SetSuccessMessage("Teacher deleted successfully.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var suggestions = await teacherService.GetSearchSuggestionsAsync(term);

        return Json(suggestions);
    }
}
