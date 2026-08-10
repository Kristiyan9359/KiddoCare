using System.Net;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Tests.Integration;

public class ControllerAccessTests
{
    [Fact]
    public async Task AuthorizedPage_ShouldReturnUnauthorizedWhenUserIsNotAuthenticated()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Children");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChildrenCreate_ShouldReturnForbiddenWhenUserIsParent()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        var response = await client.GetAsync("/Children/Create");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChildrenCreate_ShouldReturnOkWhenUserIsAdmin()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("admin-user-id", Admin);

        var response = await client.GetAsync("/Children/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChildDetails_ShouldAllowTeacherToOpenOwnGroupChildOnly()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("teacher-user-id", Teacher);

        var ownGroupResponse = await client.GetAsync("/Children/Details/1");
        var otherGroupResponse = await client.GetAsync("/Children/Details/2");

        Assert.Equal(HttpStatusCode.OK, ownGroupResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherGroupResponse.StatusCode);
    }

    [Fact]
    public async Task ChildDetails_ShouldAllowParentToOpenOwnChildOnly()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        var ownChildResponse = await client.GetAsync("/Children/Details/1");
        var otherParentChildResponse = await client.GetAsync("/Children/Details/2");

        Assert.Equal(HttpStatusCode.OK, ownChildResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherParentChildResponse.StatusCode);
    }

    [Fact]
    public async Task EventDetails_ShouldNotAllowTeacherToOpenOtherGroupEvent()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("teacher-user-id", Teacher);

        var ownGroupResponse = await client.GetAsync("/Events/Details/1");
        var otherGroupResponse = await client.GetAsync("/Events/Details/2");

        Assert.Equal(HttpStatusCode.OK, ownGroupResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherGroupResponse.StatusCode);
    }

    [Fact]
    public async Task DailyReportDetails_ShouldNotAllowTeacherToOpenOtherTeacherReport()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("teacher-user-id", Teacher);

        var ownReportResponse = await client.GetAsync("/DailyReports/Details/1");
        var otherTeacherReportResponse = await client.GetAsync("/DailyReports/Details/2");

        Assert.Equal(HttpStatusCode.OK, ownReportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherTeacherReportResponse.StatusCode);
    }

    [Fact]
    public async Task MedicalRecordsCreate_ShouldReturnForbiddenWhenUserIsParent()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        var response = await client.GetAsync("/MedicalRecords/Create");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChildDocumentsCreate_ShouldReturnForbiddenWhenUserIsTeacher()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("teacher-user-id", Teacher);

        var response = await client.GetAsync("/ChildDocuments/Create");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChildDocumentDetails_ShouldReturnNotFoundWhenParentOpensOtherParentDocument()
    {
        await using var factory = new KiddoCareWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateAuthenticatedClient("parent-user-id", Parent);

        var response = await client.GetAsync("/ChildDocuments/Details/2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public static class ControllerAccessTestExtensions
{
    public static HttpClient CreateAuthenticatedClient(
        this KiddoCareWebApplicationFactory factory,
        string userId,
        string role)
    {
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);

        return client;
    }
}
