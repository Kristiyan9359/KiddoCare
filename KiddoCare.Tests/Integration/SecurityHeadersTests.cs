using System.Net;

namespace KiddoCare.Tests.Integration;

public class SecurityHeadersTests
{
    [Fact]
    public async Task GetRequest_ShouldReturnSecurityHeaders()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }
}
