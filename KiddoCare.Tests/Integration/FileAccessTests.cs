using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Tests.Integration;

public class FileAccessTests
{
    [Fact]
    public async Task ChildDocumentDownload_ShouldReturnFileWhenUserCanAccessDocumentAndPathIsSafe()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var filePath = await CreateTestFileAsync(factory, "App_Data", "uploads", "child-documents", "own.pdf");
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        try
        {
            var response = await client.GetAsync("/ChildDocuments/Download/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        }
        finally
        {
            DeleteTestFile(filePath);
        }
    }

    [Fact]
    public async Task ChildDocumentDownload_ShouldReturnNotFoundWhenParentCannotAccessDocument()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var filePath = await CreateTestFileAsync(factory, "App_Data", "uploads", "child-documents", "other.pdf");
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        try
        {
            var response = await client.GetAsync("/ChildDocuments/Download/2");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            DeleteTestFile(filePath);
        }
    }

    [Fact]
    public async Task ChildDocumentDownload_ShouldReturnNotFoundWhenDocumentPathEscapesUploadsFolder()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var filePath = await CreateTestFileAsync(factory, "App_Data", "uploads", "secret.pdf");
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        try
        {
            var response = await client.GetAsync("/ChildDocuments/Download/3");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            DeleteTestFile(filePath);
        }
    }

    [Fact]
    public async Task ChildPhoto_ShouldReturnFileWhenUserCanAccessChildAndPathIsSafe()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var filePath = await CreateTestFileAsync(factory, "App_Data", "uploads", "child-photos", "ivan.jpg");
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        try
        {
            var response = await client.GetAsync("/Children/Photo/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        }
        finally
        {
            DeleteTestFile(filePath);
        }
    }

    [Fact]
    public async Task ChildPhoto_ShouldReturnNotFoundWhenParentCannotAccessChild()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var filePath = await CreateTestFileAsync(factory, "App_Data", "uploads", "child-photos", "maria.jpg");
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        try
        {
            var response = await client.GetAsync("/Children/Photo/2");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            DeleteTestFile(filePath);
        }
    }

    [Fact]
    public async Task ChildPhoto_ShouldReturnNotFoundWhenPhotoPathEscapesUploadsFolder()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var filePath = await CreateTestFileAsync(factory, "App_Data", "uploads", "secret.jpg");
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        try
        {
            var response = await client.GetAsync("/Children/Photo/3");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            DeleteTestFile(filePath);
        }
    }

    private static async Task<string> CreateTestFileAsync(
        KiddoCareWebApplicationFactory factory,
        params string[] pathSegments)
    {
        using var scope = factory.Services.CreateScope();
        var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(new[] { webHostEnvironment.ContentRootPath }.Concat(pathSegments).ToArray());
        var directoryPath = Path.GetDirectoryName(filePath)!;

        Directory.CreateDirectory(directoryPath);
        await File.WriteAllTextAsync(filePath, "test file");

        return filePath;
    }

    private static void DeleteTestFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
