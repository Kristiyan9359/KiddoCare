namespace KiddoCare.Web.Services;

using KiddoCare.Web.Services.Contracts;
using KiddoCare.Web.Services.Models;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxPhotoSize = 5 * 1024 * 1024;
    private const long MaxDocumentSize = 5 * 1024 * 1024;

    private const string ChildPhotosFolder = "child-photos";
    private const string ChildDocumentsFolder = "child-documents";

    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png"
    };

    private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly HashSet<string> AllowedDocumentContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private readonly IWebHostEnvironment webHostEnvironment;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
    {
        this.webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> SaveChildPhotoAsync(IFormFile photo)
    {
        ValidateUploadedFile(
            photo,
            MaxPhotoSize,
            AllowedPhotoExtensions,
            AllowedPhotoContentTypes,
            "Photo file is required.",
            "Photo file cannot be larger than 5 MB.",
            "Allowed photo formats are JPG and PNG.",
            "Uploaded photo content type is not supported.");

        return await SaveFileAsync(photo, ChildPhotosFolder);
    }

    public async Task<string> SaveChildDocumentAsync(IFormFile file)
    {
        ValidateUploadedFile(
            file,
            MaxDocumentSize,
            AllowedDocumentExtensions,
            AllowedDocumentContentTypes,
            "Document file is required.",
            "Document file cannot be larger than 5 MB.",
            "Allowed document formats are PDF, JPG and PNG.",
            "Uploaded document content type is not supported.");

        return await SaveFileAsync(file, ChildDocumentsFolder);
    }

    public StoredFileResult? GetChildPhoto(string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            return null;
        }

        if (IsRemoteUrl(photoUrl))
        {
            return new StoredFileResult
            {
                RedirectUrl = photoUrl,
                ContentType = "application/octet-stream"
            };
        }

        var filePath = GetStoredFilePath(photoUrl, ChildPhotosFolder);

        if (filePath == null)
        {
            return null;
        }

        return new StoredFileResult
        {
            FilePath = filePath,
            ContentType = GetContentType(filePath)
        };
    }

    public StoredFileResult? GetChildDocument(string? fileUrl, string downloadName)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return null;
        }

        var filePath = GetStoredFilePath(fileUrl, ChildDocumentsFolder);

        if (filePath == null)
        {
            return null;
        }

        return new StoredFileResult
        {
            FilePath = filePath,
            ContentType = GetContentType(filePath),
            DownloadName = $"{downloadName}{Path.GetExtension(filePath)}"
        };
    }

    public void DeleteChildPhoto(string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl) || IsRemoteUrl(photoUrl))
        {
            return;
        }

        var filePath = GetStoredFilePath(photoUrl, ChildPhotosFolder);

        if (filePath == null)
        {
            return;
        }

        File.Delete(filePath);
    }

    private async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        var extension = Path.GetExtension(file.FileName);
        var uploadsFolder = GetUploadsFolder(folderName);

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/App_Data/uploads/{folderName}/{fileName}";
    }

    private string? GetStoredFilePath(string fileUrl, string folderName)
    {
        var uploadsFolder = GetUploadsFolder(folderName);
        var uploadsFolderPrefix = uploadsFolder + Path.DirectorySeparatorChar;

        var filePath = Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        if (!filePath.StartsWith(uploadsFolderPrefix, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(filePath))
        {
            return null;
        }

        return filePath;
    }

    private string GetUploadsFolder(string folderName)
        => Path.GetFullPath(Path.Combine(
            webHostEnvironment.ContentRootPath,
            "App_Data",
            "uploads",
            folderName));

    private static void ValidateUploadedFile(
        IFormFile file,
        long maxFileSize,
        HashSet<string> allowedExtensions,
        HashSet<string> allowedContentTypes,
        string requiredMessage,
        string maxSizeMessage,
        string invalidExtensionMessage,
        string invalidContentTypeMessage)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException(requiredMessage);
        }

        if (file.Length > maxFileSize)
        {
            throw new InvalidOperationException(maxSizeMessage);
        }

        var extension = Path.GetExtension(file.FileName);

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(invalidExtensionMessage);
        }

        if (!allowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException(invalidContentTypeMessage);
        }
    }

    private static bool IsRemoteUrl(string fileUrl)
        => Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

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
}
