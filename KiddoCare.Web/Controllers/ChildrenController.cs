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
    private const long MaxPhotoSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly IChildService childService;
    private readonly IWebHostEnvironment webHostEnvironment;

    public ChildrenController(IChildService childService, IWebHostEnvironment webHostEnvironment)
    {
        this.childService = childService;
        this.webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    [Authorize(Roles = $"{Admin},{Teacher}")]
    public async Task<IActionResult> Index(string? searchTerm, string? medicalFilter, int page = 1, int pageSize = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childService.GetAllAsync(userId, isAdmin, isTeacher, searchTerm, medicalFilter, page, pageSize);

        return View(model);
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

            return View(model);
        }

        try
        {
            if (model.Photo != null)
            {
                model.PhotoUrl = await SavePhotoAsync(model.Photo);
            }

            await childService.CreateAsync(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await childService.GetCreateModelAsync();
            model.Groups = createModel.Groups;
            model.Parents = createModel.Parents;

            return View(model);
        }

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
            model.Parents = editModel.Parents;

            return View(model);
        }

        string? uploadedPhotoUrl = null;

        try
        {
            var previousPhotoUrl = model.PhotoUrl;

            if (model.Photo != null)
            {
                uploadedPhotoUrl = await SavePhotoAsync(model.Photo);
                model.PhotoUrl = uploadedPhotoUrl;
            }
            else if (model.RemovePhoto)
            {
                model.PhotoUrl = null;
            }

            await childService.EditAsync(model);

            if (model.Photo != null || model.RemovePhoto)
            {
                DeletePhotoFile(previousPhotoUrl);
            }
        }
        catch (InvalidOperationException ex)
        {
            DeletePhotoFile(uploadedPhotoUrl);

            ModelState.AddModelError(string.Empty, ex.Message);

            var editModel = await childService.GetForEditAsync(model.Id);

            if (editModel == null)
            {
                return NotFound();
            }

            model.PhotoUrl = editModel.PhotoUrl;
            model.Groups = editModel.Groups;
            model.Parents = editModel.Parents;

            return View(model);
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

        if (Uri.TryCreate(model.PhotoUrl, UriKind.Absolute, out var photoUri) &&
            (photoUri.Scheme == Uri.UriSchemeHttp || photoUri.Scheme == Uri.UriSchemeHttps))
        {
            return Redirect(model.PhotoUrl);
        }

        var uploadsFolder = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            "App_Data",
            "uploads",
            "child-photos"));
        var uploadsFolderPrefix = uploadsFolder + Path.DirectorySeparatorChar;

        var filePath = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            model.PhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        if (!filePath.StartsWith(uploadsFolderPrefix, StringComparison.OrdinalIgnoreCase) ||
            !System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, GetPhotoContentType(filePath));
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

    private async Task<string> SavePhotoAsync(IFormFile photo)
    {
        if (photo.Length == 0)
        {
            throw new InvalidOperationException("Photo file is required.");
        }

        if (photo.Length > MaxPhotoSize)
        {
            throw new InvalidOperationException("Photo file cannot be larger than 5 MB.");
        }

        var extension = Path.GetExtension(photo.FileName);

        if (!AllowedPhotoExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Allowed photo formats are JPG and PNG.");
        }

        var uploadsFolder = Path.Combine(
            webHostEnvironment.ContentRootPath,
            "App_Data",
            "uploads",
            "child-photos");

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await photo.CopyToAsync(stream);

        return $"/App_Data/uploads/child-photos/{fileName}";
    }

    private void DeletePhotoFile(string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            return;
        }

        if (Uri.TryCreate(photoUrl, UriKind.Absolute, out var photoUri) &&
            (photoUri.Scheme == Uri.UriSchemeHttp || photoUri.Scheme == Uri.UriSchemeHttps))
        {
            return;
        }

        var uploadsFolder = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            "App_Data",
            "uploads",
            "child-photos"));
        var uploadsFolderPrefix = uploadsFolder + Path.DirectorySeparatorChar;

        var filePath = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            photoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        if (!filePath.StartsWith(uploadsFolderPrefix, StringComparison.OrdinalIgnoreCase) ||
            !System.IO.File.Exists(filePath))
        {
            return;
        }

        System.IO.File.Delete(filePath);
    }

    private static string GetPhotoContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
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
