namespace KiddoCare.Web.Services.Models;

public class StoredFileResult
{
    public string? RedirectUrl { get; set; }

    public string? FilePath { get; set; }

    public string ContentType { get; set; } = null!;

    public string? DownloadName { get; set; }

    public bool IsRemoteFile => !string.IsNullOrWhiteSpace(RedirectUrl);
}
