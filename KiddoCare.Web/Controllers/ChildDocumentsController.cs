using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ChildDocuments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ChildDocumentsController : Controller
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly IChildDocumentService childDocumentService;
    private readonly IWebHostEnvironment webHostEnvironment;

    public ChildDocumentsController(IChildDocumentService childDocumentService, IWebHostEnvironment webHostEnvironment)
    {
        this.childDocumentService = childDocumentService;
        this.webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, int page = 1, int pageSize = 15)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetAllAsync(userId, isAdmin, isTeacher, searchTerm, statusFilter, page, pageSize);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var suggestions = await childDocumentService.GetSearchSuggestionsAsync(term, userId, isAdmin, isTeacher);

        return Json(suggestions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetDetailsAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        var uploadsFolder = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            "App_Data",
            "uploads",
            "child-documents"));
        var uploadsFolderPrefix = uploadsFolder + Path.DirectorySeparatorChar;

        var filePath = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            model.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        if (!filePath.StartsWith(uploadsFolderPrefix, StringComparison.OrdinalIgnoreCase) ||
            !System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var fileName = $"{model.Title}{Path.GetExtension(filePath)}";

        return PhysicalFile(filePath, GetContentType(filePath), fileName);
    }

    [Authorize(Roles = $"{Admin},{Parent}")]
    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = $"{Admin},{Parent}")]
    [HttpPost]
    public async Task<IActionResult> Create(ChildDocumentCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        try
        {
            model.FileUrl = await SaveDocumentFileAsync(model.File);

            await childDocumentService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await childDocumentService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;
            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(model);
        }

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    private async Task<string> SaveDocumentFileAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Document file is required.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Document file cannot be larger than 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedFileExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Allowed document formats are PDF, JPG and PNG.");
        }

        var uploadsFolder = Path.Combine(
            webHostEnvironment.ContentRootPath,
            "App_Data",
            "uploads",
            "child-documents");

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/App_Data/uploads/child-documents/{fileName}";
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Review(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await childDocumentService.GetForReviewAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        model.ReturnUrl = GetSafeReturnUrl(returnUrl);

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Review(ChildDocumentReviewViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        ModelState.Remove(nameof(ChildDocumentReviewViewModel.ChildFullName));
        ModelState.Remove(nameof(ChildDocumentReviewViewModel.Title));
        ModelState.Remove(nameof(ChildDocumentReviewViewModel.FileUrl));

        if (!ModelState.IsValid)
        {
            var reviewModel = await childDocumentService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.Status = model.Status;
            reviewModel.ReviewNote = model.ReviewNote;
            reviewModel.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(reviewModel);
        }

        try
        {
            await childDocumentService.ReviewAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var reviewModel = await childDocumentService.GetForReviewAsync(model.Id, userId, isAdmin, isTeacher);

            if (reviewModel == null)
            {
                return NotFound();
            }

            reviewModel.Status = model.Status;
            reviewModel.ReviewNote = model.ReviewNote;
            reviewModel.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);

            return View(reviewModel);
        }

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    private IActionResult RedirectToLocalOrIndex(string? returnUrl)
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        if (safeReturnUrl != null)
        {
            return Redirect(safeReturnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
