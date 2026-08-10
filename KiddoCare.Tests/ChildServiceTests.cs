using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Children;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class ChildServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllActiveChildrenWhenUserIsAdmin()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var result = (await service.GetAllAsync("admin-user-id", isAdmin: true, isTeacher: false, medicalFilter: null)).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(result, c => c.FullName == "Maria Petrova");
        Assert.Contains(result, c => c.FullName == "Georgi Georgiev");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyTeacherGroupChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var result = (await service.GetAllAsync("teacher-user-id", isAdmin: false, isTeacher: true, medicalFilter: null)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(result, c => c.FullName == "Georgi Georgiev");
        Assert.DoesNotContain(result, c => c.FullName == "Maria Petrova");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyParentChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var result = (await service.GetAllAsync("parent-user-id", isAdmin: false, isTeacher: false, medicalFilter: null)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(result, c => c.FullName == "Georgi Georgiev");
        Assert.DoesNotContain(result, c => c.FullName == "Maria Petrova");
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyWithRecordsMedicalFilter()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var result = (await service.GetAllAsync("admin-user-id", isAdmin: true, isTeacher: false, medicalFilter: "with-records")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(result, c => c.FullName == "Maria Petrova");
        Assert.DoesNotContain(result, c => c.FullName == "Georgi Georgiev");
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyWithAllergiesMedicalFilter()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var result = (await service.GetAllAsync("admin-user-id", isAdmin: true, isTeacher: false, medicalFilter: "with-allergies")).ToList();

        Assert.Single(result);
        Assert.Equal("Ivan Ivanov", result[0].FullName);
    }

    [Fact]
    public async Task CanAccessChildAsync_ShouldAllowTeacherToAccessOwnGroupChildOnly()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var canAccessOwnGroupChild = await service.CanAccessChildAsync(1, "teacher-user-id", isAdmin: false, isTeacher: true);
        var canAccessOtherGroupChild = await service.CanAccessChildAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.True(canAccessOwnGroupChild);
        Assert.False(canAccessOtherGroupChild);
    }

    [Fact]
    public async Task CanAccessChildAsync_ShouldAllowParentToAccessOwnChildOnly()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var canAccessOwnChild = await service.CanAccessChildAsync(1, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessOtherParentChild = await service.CanAccessChildAsync(2, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.True(canAccessOwnChild);
        Assert.False(canAccessOtherParentChild);
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnChildDetailsWithMedicalSummaryAndRecentItems()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var model = await service.GetDetailsAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Ivan Ivanov", model!.FullName);
        Assert.True(model.HasMedicalRecord);
        Assert.Equal("Peanuts", model.MedicalAllergies);
        Assert.Single(model.RecentAbsenceRequests);
        Assert.Single(model.RecentConsentRequests);
        Assert.Single(model.RecentDocuments);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);
        var model = new ChildCreateViewModel
        {
            FirstName = "Nikol",
            LastName = "Nikolova",
            Gender = Gender.Female,
            DateOfBirth = DateTime.Today.AddYears(-4),
            GroupId = 1,
            ParentId = 1,
            PhotoUrl = "/images/nikol.jpg"
        };

        await service.CreateAsync(model);

        var child = await context.Children
            .FirstOrDefaultAsync(c => c.FirstName == "Nikol" && !c.IsDeleted);

        Assert.NotNull(child);
        Assert.Equal(1, child!.GroupId);
        Assert.Equal(1, child.ParentId);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);
        var model = new ChildEditViewModel
        {
            Id = 1,
            FirstName = "Updated",
            LastName = "Child",
            Gender = Gender.Male,
            DateOfBirth = DateTime.Today.AddYears(-5),
            GroupId = 2,
            ParentId = 2,
            PhotoUrl = "/images/updated.jpg"
        };

        await service.EditAsync(model);

        var child = await context.Children.FindAsync(1);

        Assert.Equal("Updated", child!.FirstName);
        Assert.Equal("Child", child.LastName);
        Assert.Equal(2, child.GroupId);
        Assert.Equal(2, child.ParentId);
        Assert.Equal("/images/updated.jpg", child.PhotoUrl);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        await service.DeleteAsync(1);

        var child = await context.Children.FindAsync(1);

        Assert.True(child!.IsDeleted);
    }

    [Fact]
    public async Task GetCreateModelAsync_ShouldReturnActiveGroupsAndParents()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ChildService(context);

        var model = await service.GetCreateModelAsync();

        Assert.Equal(2, model.Groups.Count());
        Assert.Equal(2, model.Parents.Count());
        Assert.Contains(model.Parents, p => p.Text.Contains("Parent One"));
        Assert.DoesNotContain(model.Parents, p => p.Text.Contains("Deleted Parent"));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedDataAsync(ApplicationDbContext context)
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
            },
            new KindergartenGroup
            {
                Id = 3,
                Name = "Deleted Group",
                IsDeleted = true
            });

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "admin-user-id",
                UserName = "admin@kiddocare.com",
                Email = "admin@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "teacher-user-id",
                UserName = "teacher@kiddocare.com",
                Email = "teacher@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "parent-user-id",
                UserName = "parent@kiddocare.com",
                Email = "parent@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "other-parent-user-id",
                UserName = "other-parent@kiddocare.com",
                Email = "other-parent@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "deleted-parent-user-id",
                UserName = "deleted-parent@kiddocare.com",
                Email = "deleted-parent@kiddocare.com"
            });

        context.TeacherProfiles.Add(new TeacherProfile
        {
            Id = 1,
            UserId = "teacher-user-id",
            FullName = "Teacher One",
            GroupId = 1
        });

        context.ParentProfiles.AddRange(
            new ParentProfile
            {
                Id = 1,
                UserId = "parent-user-id",
                FullName = "Parent One",
                PhoneNumber = "0888111111"
            },
            new ParentProfile
            {
                Id = 2,
                UserId = "other-parent-user-id",
                FullName = "Parent Two",
                PhoneNumber = "0888222222"
            },
            new ParentProfile
            {
                Id = 3,
                UserId = "deleted-parent-user-id",
                FullName = "Deleted Parent",
                IsDeleted = true
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
                ParentId = 1,
                PhotoUrl = "/images/ivan.jpg"
            },
            new Child
            {
                Id = 2,
                FirstName = "Maria",
                LastName = "Petrova",
                Gender = Gender.Female,
                DateOfBirth = DateTime.Today.AddYears(-3),
                GroupId = 2,
                ParentId = 2
            },
            new Child
            {
                Id = 3,
                FirstName = "Georgi",
                LastName = "Georgiev",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-5),
                GroupId = 1,
                ParentId = 1
            },
            new Child
            {
                Id = 4,
                FirstName = "Deleted",
                LastName = "Child",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1,
                ParentId = 1,
                IsDeleted = true
            });

        context.MedicalRecords.AddRange(
            new MedicalRecord
            {
                Id = 1,
                ChildId = 1,
                Allergies = "Peanuts",
                ChronicConditions = "Asthma",
                EmergencyContactName = "Parent One",
                EmergencyContactPhone = "0877111111"
            },
            new MedicalRecord
            {
                Id = 2,
                ChildId = 2,
                Allergies = "",
                ChronicConditions = "None",
                EmergencyContactName = "Parent Two",
                EmergencyContactPhone = "0877222222"
            },
            new MedicalRecord
            {
                Id = 3,
                ChildId = 3,
                Allergies = "Deleted allergy",
                IsDeleted = true
            });

        context.AbsenceRequests.AddRange(
            new AbsenceRequest
            {
                Id = 1,
                ChildId = 1,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(1),
                Reason = AbsenceReason.Sick,
                Status = RequestStatus.Approved,
                RequestedByUserId = "parent-user-id"
            },
            new AbsenceRequest
            {
                Id = 2,
                ChildId = 1,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(2),
                Reason = AbsenceReason.Other,
                Status = RequestStatus.Rejected,
                RequestedByUserId = "parent-user-id"
            });

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 1,
            Title = "Photo permission",
            Type = ConsentRequestType.PhotoPermission,
            Status = RequestStatus.Pending,
            CreatedByUserId = "teacher-user-id"
        });

        context.ChildDocuments.Add(new ChildDocument
        {
            Id = 1,
            ChildId = 1,
            Type = ChildDocumentType.MedicalNote,
            Title = "Medical note",
            FileUrl = "/App_Data/uploads/child-documents/test.pdf",
            Status = RequestStatus.Approved,
            UploadedByUserId = "parent-user-id"
        });

        await context.SaveChangesAsync();
    }
}
