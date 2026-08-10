using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.ConsentRequests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class ConsentRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldNotAllowTeacherToCreateConsentForChildFromAnotherGroup()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        var service = new ConsentRequestService(context);
        var model = CreateModel(childId: 2, type: ConsentRequestType.FieldTrip);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        Assert.Empty(context.ConsentRequests);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowTeacherToCreateConsentForOwnGroup()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        var service = new ConsentRequestService(context);
        var model = CreateModel(childId: 1, type: ConsentRequestType.FieldTrip);

        await service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true);

        var request = await context.ConsentRequests.SingleAsync();

        Assert.Equal(1, request.ChildId);
        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Equal("teacher-user-id", request.CreatedByUserId);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotAllowParentToCreateConsentRequest()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        var service = new ConsentRequestService(context);
        var model = CreateModel(childId: 1, type: ConsentRequestType.FieldTrip);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "parent-user-id", isAdmin: false, isTeacher: false));

        Assert.Empty(context.ConsentRequests);
    }

    [Fact]
    public async Task RespondAsync_ShouldAllowParentToRespondForOwnChild()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 1,
            Title = "Photo permission",
            Type = ConsentRequestType.PhotoPermission,
            CreatedByUserId = "admin-user-id"
        });
        await context.SaveChangesAsync();

        var service = new ConsentRequestService(context);
        var model = new ConsentRequestRespondViewModel
        {
            Id = 1,
            Status = RequestStatus.Approved,
            ParentNote = "Approved."
        };

        await service.RespondAsync(model, "parent-user-id");

        var request = await context.ConsentRequests.SingleAsync();

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Equal("parent-user-id", request.RespondedByUserId);
        Assert.NotNull(request.RespondedOn);
        Assert.Equal("Approved.", request.ParentNote);
    }

    [Fact]
    public async Task RespondAsync_ShouldNotAllowParentToRespondForAnotherParentsChild()
    {
        await using var context = CreateContext();
        await SeedTwoParentsAndChildrenAsync(context);

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 2,
            Title = "Field trip",
            Type = ConsentRequestType.FieldTrip,
            CreatedByUserId = "admin-user-id"
        });
        await context.SaveChangesAsync();

        var service = new ConsentRequestService(context);
        var model = new ConsentRequestRespondViewModel
        {
            Id = 1,
            Status = RequestStatus.Approved
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RespondAsync(model, "first-parent-user-id"));

        var request = await context.ConsentRequests.SingleAsync();

        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Null(request.RespondedByUserId);
    }

    [Fact]
    public async Task CreateAsync_ShouldBlockDuplicateStandingConsentEvenWhenExistingIsApproved()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 1,
            Title = "Photo permission",
            Type = ConsentRequestType.PhotoPermission,
            Status = RequestStatus.Approved,
            CreatedByUserId = "admin-user-id"
        });
        await context.SaveChangesAsync();

        var service = new ConsentRequestService(context);
        var model = CreateModel(childId: 1, type: ConsentRequestType.PhotoPermission);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false));

        Assert.Single(context.ConsentRequests);
    }

    [Fact]
    public async Task CreateAsync_ShouldBlockDuplicatePendingNonStandingConsent()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 1,
            Title = "Field trip",
            Type = ConsentRequestType.FieldTrip,
            Status = RequestStatus.Pending,
            CreatedByUserId = "admin-user-id"
        });
        await context.SaveChangesAsync();

        var service = new ConsentRequestService(context);
        var model = CreateModel(childId: 1, type: ConsentRequestType.FieldTrip);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false));

        Assert.Single(context.ConsentRequests);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowNewNonStandingConsentAfterExistingWasResponded()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 1,
            Title = "Old field trip",
            Type = ConsentRequestType.FieldTrip,
            Status = RequestStatus.Approved,
            CreatedByUserId = "admin-user-id"
        });
        await context.SaveChangesAsync();

        var service = new ConsentRequestService(context);
        var model = CreateModel(childId: 1, type: ConsentRequestType.FieldTrip);

        await service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        Assert.Equal(2, await context.ConsentRequests.CountAsync());
    }

    private static ConsentRequestCreateViewModel CreateModel(int childId, ConsentRequestType type)
    {
        return new ConsentRequestCreateViewModel
        {
            ChildId = childId,
            Title = "Consent request",
            Description = "Please respond.",
            Type = type
        };
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedTeacherGroupsAndChildrenAsync(ApplicationDbContext context)
    {
        context.KindergartenGroups.AddRange(
            new KindergartenGroup
            {
                Id = 1,
                Name = "Sunshine"
            },
            new KindergartenGroup
            {
                Id = 2,
                Name = "Moonlight"
            });

        context.Users.Add(new IdentityUser
        {
            Id = "teacher-user-id",
            UserName = "teacher@kiddocare.com",
            Email = "teacher@kiddocare.com"
        });

        context.TeacherProfiles.Add(new TeacherProfile
        {
            Id = 1,
            UserId = "teacher-user-id",
            FullName = "Teacher One",
            GroupId = 1
        });

        context.Children.AddRange(
            new Child
            {
                Id = 1,
                FirstName = "Ivan",
                LastName = "Ivanov",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1
            },
            new Child
            {
                Id = 2,
                FirstName = "Maria",
                LastName = "Petrova",
                Gender = Gender.Female,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 2
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedParentAndChildAsync(ApplicationDbContext context)
    {
        context.KindergartenGroups.Add(new KindergartenGroup
        {
            Id = 1,
            Name = "Sunshine"
        });

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "parent-user-id",
                UserName = "parent@kiddocare.com",
                Email = "parent@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "admin-user-id",
                UserName = "admin@kiddocare.com",
                Email = "admin@kiddocare.com"
            });

        context.ParentProfiles.Add(new ParentProfile
        {
            Id = 1,
            UserId = "parent-user-id",
            FullName = "Parent One"
        });

        context.Children.Add(new Child
        {
            Id = 1,
            FirstName = "Ivan",
            LastName = "Ivanov",
            Gender = Gender.Male,
            DateOfBirth = DateTime.Today.AddYears(-4),
            GroupId = 1,
            ParentId = 1
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedTwoParentsAndChildrenAsync(ApplicationDbContext context)
    {
        context.KindergartenGroups.Add(new KindergartenGroup
        {
            Id = 1,
            Name = "Sunshine"
        });

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "first-parent-user-id",
                UserName = "first.parent@kiddocare.com",
                Email = "first.parent@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "second-parent-user-id",
                UserName = "second.parent@kiddocare.com",
                Email = "second.parent@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "admin-user-id",
                UserName = "admin@kiddocare.com",
                Email = "admin@kiddocare.com"
            });

        context.ParentProfiles.AddRange(
            new ParentProfile
            {
                Id = 1,
                UserId = "first-parent-user-id",
                FullName = "First Parent"
            },
            new ParentProfile
            {
                Id = 2,
                UserId = "second-parent-user-id",
                FullName = "Second Parent"
            });

        context.Children.AddRange(
            new Child
            {
                Id = 1,
                FirstName = "Ivan",
                LastName = "Ivanov",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1,
                ParentId = 1
            },
            new Child
            {
                Id = 2,
                FirstName = "Maria",
                LastName = "Petrova",
                Gender = Gender.Female,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1,
                ParentId = 2
            });

        await context.SaveChangesAsync();
    }
}
