namespace KiddoCare.Web.Services.Options;

public class FileStorageOptions
{
    public int MaxPhotoSizeInMb { get; set; } = 5;

    public int MaxDocumentSizeInMb { get; set; } = 5;

    public int MaxUploadRequestSizeInMb { get; set; } = 10;

    public string ChildPhotosFolder { get; set; } = "child-photos";

    public string ChildDocumentsFolder { get; set; } = "child-documents";

    public List<string> AllowedPhotoExtensions { get; set; } = new()
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    public List<string> AllowedPhotoContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png"
    };

    public List<string> AllowedDocumentExtensions { get; set; } = new()
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public List<string> AllowedDocumentContentTypes { get; set; } = new()
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    public long MaxPhotoSizeInBytes => MaxPhotoSizeInMb * 1024L * 1024L;

    public long MaxDocumentSizeInBytes => MaxDocumentSizeInMb * 1024L * 1024L;

    public long MaxUploadRequestSizeInBytes => MaxUploadRequestSizeInMb * 1024L * 1024L;
}
