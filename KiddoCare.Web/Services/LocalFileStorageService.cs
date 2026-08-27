namespace KiddoCare.Web.Services;

using KiddoCare.Web.Services.Contracts;
using KiddoCare.Web.Services.Models;
using KiddoCare.Web.Services.Options;
using Microsoft.Extensions.Options;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly FileStorageOptions fileStorageOptions;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment, IOptions<FileStorageOptions> fileStorageOptions)
    {
        this.webHostEnvironment = webHostEnvironment;
        this.fileStorageOptions = fileStorageOptions.Value;
    }

    public async Task<string> SaveChildPhotoAsync(IFormFile photo)
    {
        ValidateUploadedFile(
            photo,
            fileStorageOptions.MaxPhotoSizeInBytes,
            fileStorageOptions.AllowedPhotoExtensions,
            fileStorageOptions.AllowedPhotoContentTypes,
            "Photo file is required.",
            "Photo file cannot be larger than 5 MB.",
            "Allowed photo formats are JPG and PNG.",
            "Uploaded photo content type is not supported.");

        return await SaveFileAsync(photo, fileStorageOptions.ChildPhotosFolder);
    }

    public async Task<string> SaveChildDocumentAsync(IFormFile file)
    {
        ValidateUploadedFile(
            file,
            fileStorageOptions.MaxDocumentSizeInBytes,
            fileStorageOptions.AllowedDocumentExtensions,
            fileStorageOptions.AllowedDocumentContentTypes,
            "Document file is required.",
            "Document file cannot be larger than 5 MB.",
            "Allowed document formats are PDF, JPG and PNG.",
            "Uploaded document content type is not supported.");

        return await SaveFileAsync(file, fileStorageOptions.ChildDocumentsFolder);
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

        var filePath = GetStoredFilePath(photoUrl, fileStorageOptions.ChildPhotosFolder);

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

        var filePath = GetStoredFilePath(fileUrl, fileStorageOptions.ChildDocumentsFolder);

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

        var filePath = GetStoredFilePath(photoUrl, fileStorageOptions.ChildPhotosFolder);

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
        IEnumerable<string> allowedExtensions,
        IEnumerable<string> allowedContentTypes,
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

        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(invalidExtensionMessage);
        }

        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
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
