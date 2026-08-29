using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Groups;
using KiddoCare.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize(Roles = Admin)]
public class GroupsController : Controller
{
    private readonly IGroupService groupService;
    private readonly IStringLocalizer<SharedResource> localizer;

    public GroupsController(IGroupService groupService, IStringLocalizer<SharedResource> localizer)
    {
        this.groupService = groupService;
        this.localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? returnUrl, int page = 1, int pageSize = 15)
    {
        var model = await groupService.GetAllAsync(searchTerm, page, pageSize);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public IActionResult Create(string? returnUrl)
    {
        return View(new GroupCreateViewModel
        {
            ReturnUrl = GetSafeReturnUrl(returnUrl)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(GroupCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
            return View(model);
        }

        await groupService.CreateAsync(model);

        this.SetSuccessMessage("Group created successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var model = await groupService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(GroupEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
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

        this.SetSuccessMessage("Group updated successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var model = await groupService.GetDetailsAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var model = await groupService.GetForDeleteAsync(id);

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
            await groupService.DeleteAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);

            var model = await groupService.GetForDeleteAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            model.ReturnUrl = GetSafeReturnUrl(returnUrl);

            return View(model);
        }

        this.SetSuccessMessage("Group deleted successfully.");

        return RedirectToLocalOrIndex(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var suggestions = await groupService.GetSearchSuggestionsAsync(term);

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
