namespace KiddoCare.Web.Services.Contracts;

using KiddoCare.Web.Services.Models;

public interface IFileStorageService
{
    Task<string> SaveChildPhotoAsync(IFormFile photo);

    Task<string> SaveChildDocumentAsync(IFormFile file);

    StoredFileResult? GetChildPhoto(string? photoUrl);

    StoredFileResult? GetChildDocument(string? fileUrl, string downloadName);

    void DeleteChildPhoto(string? photoUrl);
}
