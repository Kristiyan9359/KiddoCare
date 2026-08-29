using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Parents;
using KiddoCare.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize(Roles = Admin)]
public class ParentsController : Controller
{
    private readonly IParentService parentService;
    private readonly IStringLocalizer<SharedResource> localizer;

    public ParentsController(IParentService parentService, IStringLocalizer<SharedResource> localizer)
    {
        this.parentService = parentService;
        this.localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? returnUrl, int page = 1, int pageSize = 15)
    {
        var model = await parentService.GetAllAsync(searchTerm, page, pageSize);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var model = await parentService.GetDetailsAsync(id);

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
        var model = await parentService.GetCreateModelAsync();
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ParentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
            return View(model);
        }

        try
        {
            await parentService.CreateAsync(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
            return View(model);
        }

        this.SetSuccessMessage("Parent created successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var model = await parentService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ParentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
            return View(model);
        }

        try
        {
            await parentService.EditAsync(model);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        this.SetSuccessMessage("Parent updated successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var model = await parentService.GetForDeleteAsync(id);

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
            await parentService.DeleteAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        this.SetSuccessMessage("Parent deleted successfully.");

        return RedirectToLocalOrIndex(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var suggestions = await parentService.GetSearchSuggestionsAsync(term);

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
