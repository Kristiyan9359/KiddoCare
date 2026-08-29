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
    public async Task<IActionResult> Index(string? searchTerm, string? returnUrl, int page = 1, int pageSize = 15)
    {
        var model = await teacherService.GetAllAsync(searchTerm, page, pageSize);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var model = await teacherService.GetDetailsAsync(id);

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
        var model = await teacherService.GetCreateModelAsync();
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TeacherCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Groups = (await teacherService.GetCreateModelAsync()).Groups;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
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
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
            return View(model);
        }

        this.SetSuccessMessage("Teacher created successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var model = await teacherService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

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
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
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

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var model = await teacherService.GetForDeleteAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl)
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

        return RedirectToLocalOrIndex(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var suggestions = await teacherService.GetSearchSuggestionsAsync(term);

        return Json(suggestions);
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

        return safeReturnUrl != null
            ? LocalRedirect(safeReturnUrl)
            : RedirectToAction(nameof(Index));
    }
}
