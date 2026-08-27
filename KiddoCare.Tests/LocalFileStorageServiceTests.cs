using KiddoCare.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace KiddoCare.Tests;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string contentRootPath;
    private readonly LocalFileStorageService fileStorageService;

    public LocalFileStorageServiceTests()
    {
        contentRootPath = Path.Combine(Path.GetTempPath(), "KiddoCareStorageTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(contentRootPath);

        fileStorageService = new LocalFileStorageService(new TestWebHostEnvironment(contentRootPath));
    }

    [Fact]
    public async Task SaveChildPhotoAsync_ShouldSaveValidPhotoAndReturnLocalUrl()
    {
        var photo = CreateFormFile("child.jpg", "image/jpeg");

        var photoUrl = await fileStorageService.SaveChildPhotoAsync(photo);

        Assert.StartsWith("/App_Data/uploads/child-photos/", photoUrl);
        Assert.EndsWith(".jpg", photoUrl);
        Assert.True(File.Exists(GetPhysicalPath(photoUrl)));
    }

    [Fact]
    public async Task SaveChildPhotoAsync_ShouldThrowWhenContentTypeIsNotSupported()
    {
        var photo = CreateFormFile("child.jpg", "text/plain");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fileStorageService.SaveChildPhotoAsync(photo));

        Assert.Equal("Uploaded photo content type is not supported.", exception.Message);
    }

    [Fact]
    public async Task SaveChildDocumentAsync_ShouldSaveValidDocumentAndReturnLocalUrl()
    {
        var document = CreateFormFile("document.pdf", "application/pdf");

        var documentUrl = await fileStorageService.SaveChildDocumentAsync(document);

        Assert.StartsWith("/App_Data/uploads/child-documents/", documentUrl);
        Assert.EndsWith(".pdf", documentUrl);
        Assert.True(File.Exists(GetPhysicalPath(documentUrl)));
    }

    [Fact]
    public async Task SaveChildDocumentAsync_ShouldThrowWhenExtensionIsNotSupported()
    {
        var document = CreateFormFile("document.exe", "application/pdf");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fileStorageService.SaveChildDocumentAsync(document));

        Assert.Equal("Allowed document formats are PDF, JPG and PNG.", exception.Message);
    }

    [Fact]
    public void GetChildPhoto_ShouldReturnStoredFileWhenPathIsSafe()
    {
        var photoUrl = CreateStoredFile("child-photos", "child.png");

        var result = fileStorageService.GetChildPhoto(photoUrl);

        Assert.NotNull(result);
        Assert.False(result.IsRemoteFile);
        Assert.Equal("image/png", result.ContentType);
        Assert.True(File.Exists(result.FilePath));
    }

    [Fact]
    public void GetChildPhoto_ShouldReturnRemoteFileWhenPhotoUrlIsRemote()
    {
        const string photoUrl = "https://example.com/child.jpg";

        var result = fileStorageService.GetChildPhoto(photoUrl);

        Assert.NotNull(result);
        Assert.True(result.IsRemoteFile);
        Assert.Equal(photoUrl, result.RedirectUrl);
    }

    [Fact]
    public void GetChildPhoto_ShouldReturnNullWhenPathEscapesUploadsFolder()
    {
        CreateStoredFile("secret.jpg");

        var result = fileStorageService.GetChildPhoto("/App_Data/uploads/child-photos/../secret.jpg");

        Assert.Null(result);
    }

    [Fact]
    public void GetChildDocument_ShouldReturnStoredFileWithDownloadName()
    {
        var documentUrl = CreateStoredFile("child-documents", "medical.pdf");

        var result = fileStorageService.GetChildDocument(documentUrl, "Medical Record");

        Assert.NotNull(result);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("Medical Record.pdf", result.DownloadName);
        Assert.True(File.Exists(result.FilePath));
    }

    [Fact]
    public void DeleteChildPhoto_ShouldDeleteStoredPhotoWhenPathIsSafe()
    {
        var photoUrl = CreateStoredFile("child-photos", "child.jpg");
        var photoPath = GetPhysicalPath(photoUrl);

        fileStorageService.DeleteChildPhoto(photoUrl);

        Assert.False(File.Exists(photoPath));
    }

    [Fact]
    public void DeleteChildPhoto_ShouldIgnoreRemotePhotoUrl()
    {
        var exception = Record.Exception(() =>
            fileStorageService.DeleteChildPhoto("https://example.com/child.jpg"));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        if (Directory.Exists(contentRootPath))
        {
            Directory.Delete(contentRootPath, recursive: true);
        }
    }

    private static IFormFile CreateFormFile(string fileName, string contentType)
    {
        var stream = new MemoryStream("test file"u8.ToArray());

        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private string CreateStoredFile(params string[] pathSegments)
    {
        var filePath = Path.Combine(new[] { contentRootPath, "App_Data", "uploads" }.Concat(pathSegments).ToArray());
        var directoryPath = Path.GetDirectoryName(filePath)!;

        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(filePath, "test file");

        return $"/App_Data/uploads/{string.Join('/', pathSegments)}";
    }

    private string GetPhysicalPath(string fileUrl)
        => Path.GetFullPath(Path.Combine(
            contentRootPath,
            fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");

            Directory.CreateDirectory(WebRootPath);

            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
        }

        public string ApplicationName { get; set; } = "KiddoCare.Tests";

        public IFileProvider ContentRootFileProvider { get; set; }

        public string ContentRootPath { get; set; }

        public string EnvironmentName { get; set; } = "Development";

        public IFileProvider WebRootFileProvider { get; set; }

        public string WebRootPath { get; set; }
    }
}
