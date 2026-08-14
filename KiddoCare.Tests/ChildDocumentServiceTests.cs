using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.ChildDocuments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class ChildDocumentServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldApproveAdminUploadedDocument()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        var service = new ChildDocumentService(context);
        var model = CreateModel(childId: 1);

        await service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var document = await context.ChildDocuments.SingleAsync();

        Assert.Equal(RequestStatus.Approved, document.Status);
        Assert.Equal("admin-user-id", document.ReviewedByUserId);
        Assert.NotNull(document.ReviewedOn);
        Assert.Equal("Approved on upload.", document.ReviewNote);
    }

    [Fact]
    public async Task CreateAsync_ShouldKeepParentUploadedDocumentPending()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        var service = new ChildDocumentService(context);
        var model = CreateModel(childId: 1);

        await service.CreateAsync(model, "parent-user-id", isAdmin: false, isTeacher: false);

        var document = await context.ChildDocuments.SingleAsync();

        Assert.Equal(RequestStatus.Pending, document.Status);
        Assert.Null(document.ReviewedByUserId);
        Assert.Null(document.ReviewedOn);
        Assert.Null(document.ReviewNote);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotAllowParentToUploadForAnotherParentsChild()
    {
        await using var context = CreateContext();
        await SeedTwoParentsAndChildrenAsync(context);

        var service = new ChildDocumentService(context);
        var model = CreateModel(childId: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "first-parent-user-id", isAdmin: false, isTeacher: false));

        Assert.Empty(context.ChildDocuments);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyTeacherGroupDocuments()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndDocumentsAsync(context);

        var service = new ChildDocumentService(context);

        var result = await service.GetAllAsync("teacher-user-id", isAdmin: false, isTeacher: true, searchTerm: null, statusFilter: null, page: 1, pageSize: 15);

        var document = Assert.Single(result.Documents);

        Assert.Equal("Ivan Ivanov", document.ChildFullName);
        Assert.Equal("Sunshine", document.GroupName);
    }

    [Fact]
    public async Task ReviewAsync_ShouldAllowAdminToReviewPendingDocument()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.ChildDocuments.Add(new ChildDocument
        {
            Id = 1,
            ChildId = 1,
            Type = ChildDocumentType.MedicalNote,
            Title = "Medical note",
            FileUrl = "/App_Data/uploads/child-documents/test.pdf",
            UploadedByUserId = "parent-user-id"
        });
        await context.SaveChangesAsync();

        var service = new ChildDocumentService(context);
        var model = new ChildDocumentReviewViewModel
        {
            Id = 1,
            Status = RequestStatus.Approved,
            ReviewNote = "Looks good."
        };

        await service.ReviewAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var document = await context.ChildDocuments.SingleAsync();

        Assert.Equal(RequestStatus.Approved, document.Status);
        Assert.Equal("admin-user-id", document.ReviewedByUserId);
        Assert.NotNull(document.ReviewedOn);
        Assert.Equal("Looks good.", document.ReviewNote);
    }

    private static ChildDocumentCreateViewModel CreateModel(int childId)
    {
        return new ChildDocumentCreateViewModel
        {
            ChildId = childId,
            Type = ChildDocumentType.MedicalNote,
            Title = "Medical note",
            FileUrl = "/App_Data/uploads/child-documents/test.pdf"
        };
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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

    private static async Task SeedTeacherGroupsChildrenAndDocumentsAsync(ApplicationDbContext context)
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

        context.ChildDocuments.AddRange(
            new ChildDocument
            {
                Id = 1,
                ChildId = 1,
                Type = ChildDocumentType.MedicalNote,
                Title = "Ivan medical note",
                FileUrl = "/App_Data/uploads/child-documents/ivan.pdf",
                UploadedByUserId = "parent-user-id"
            },
            new ChildDocument
            {
                Id = 2,
                ChildId = 2,
                Type = ChildDocumentType.MedicalNote,
                Title = "Maria medical note",
                FileUrl = "/App_Data/uploads/child-documents/maria.pdf",
                UploadedByUserId = "parent-user-id"
            });

        await context.SaveChangesAsync();
    }
}
