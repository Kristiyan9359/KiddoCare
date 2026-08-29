using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Children;
using KiddoCare.Web.Extensions;
using KiddoCare.Web.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ChildrenController : Controller
{
    private readonly IChildService childService;
    private readonly IFileStorageService fileStorageService;
    private readonly IStringLocalizer<SharedResource> localizer;

    public ChildrenController(IChildService childService, IFileStorageService fileStorageService, IStringLocalizer<SharedResource> localizer)
    {
        this.childService = childService;
        this.fileStorageService = fileStorageService;
        this.localizer = localizer;
    }

    [HttpGet]
    [Authorize(Roles = $"{Admin},{Teacher}")]
    public async Task<IActionResult> Index(string? searchTerm, string? medicalFilter, string? returnUrl, int page = 1, int pageSize = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childService.GetAllAsync(userId, isAdmin, isTeacher, searchTerm, medicalFilter, page, pageSize);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl)
    {
        var model = await childService.GetCreateModelAsync();
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

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
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            if (model.Photo != null)
            {
                model.PhotoUrl = await fileStorageService.SaveChildPhotoAsync(model.Photo);
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(ChildCreateViewModel.Photo), this.localizer[ex.Message]);

            var createModel = await childService.GetCreateModelAsync();
            model.Groups = createModel.Groups;
            model.Parents = createModel.Parents;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            await childService.CreateAsync(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);

            var createModel = await childService.GetCreateModelAsync();
            model.Groups = createModel.Groups;
            model.Parents = createModel.Parents;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        this.SetSuccessMessage("Child created successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var model = await childService.GetForEditAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

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
            model.Parents = editModel.Parents;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        string? uploadedPhotoUrl = null;
        var previousPhotoUrl = model.PhotoUrl;

        try
        {
            if (model.Photo != null)
            {
                uploadedPhotoUrl = await fileStorageService.SaveChildPhotoAsync(model.Photo);
                model.PhotoUrl = uploadedPhotoUrl;
            }
            else if (model.RemovePhoto)
            {
                model.PhotoUrl = null;
            }
        }
        catch (InvalidOperationException ex)
        {
            fileStorageService.DeleteChildPhoto(uploadedPhotoUrl);

            ModelState.AddModelError(nameof(ChildEditViewModel.Photo), this.localizer[ex.Message]);

            var editModel = await childService.GetForEditAsync(model.Id);

            if (editModel == null)
            {
                return NotFound();
            }

            model.PhotoUrl = editModel.PhotoUrl;
            model.Groups = editModel.Groups;
            model.Parents = editModel.Parents;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            await childService.EditAsync(model);

            if (model.Photo != null || model.RemovePhoto)
            {
                fileStorageService.DeleteChildPhoto(previousPhotoUrl);
            }
        }
        catch (InvalidOperationException ex)
        {
            fileStorageService.DeleteChildPhoto(uploadedPhotoUrl);

            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);

            var editModel = await childService.GetForEditAsync(model.Id);

            if (editModel == null)
            {
                return NotFound();
            }

            model.PhotoUrl = editModel.PhotoUrl;
            model.Groups = editModel.Groups;
            model.Parents = editModel.Parents;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        this.SetSuccessMessage("Child updated successfully.");

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var model = await childService.GetForDeleteAsync(id);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl)
    {
        try
        {
            await childService.DeleteAsync(id);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        this.SetSuccessMessage("Child deleted successfully.");

        return RedirectToLocalOrIndex(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var canAccess = await childService.CanAccessChildAsync(id, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return NotFound();
        }

        var model = await childService.GetDetailsAsync(id);

        if (model == null || string.IsNullOrWhiteSpace(model.PhotoUrl))
        {
            return NotFound();
        }

        var storedFile = fileStorageService.GetChildPhoto(model.PhotoUrl);

        if (storedFile == null)
        {
            return NotFound();
        }

        if (storedFile.IsRemoteFile)
        {
            return Redirect(storedFile.RedirectUrl!);
        }

        return PhysicalFile(storedFile.FilePath!, storedFile.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
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

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
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

    [HttpGet]
    [Authorize(Roles = $"{Admin},{Teacher}")]
    public async Task<IActionResult> Suggestions(string term)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var suggestions = await childService.GetSearchSuggestionsAsync(term, userId, isAdmin, isTeacher);

        return Json(suggestions);
    }
}
