using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.DailyReports;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class DailyReportServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldNotAllowTeacherToCreateReportForChildFromAnotherGroup()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        var service = new DailyReportService(context);
        var model = CreateModel(childId: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        Assert.Empty(context.DailyReports);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowTeacherToCreateReportForOwnGroup()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        var service = new DailyReportService(context);
        var model = CreateModel(childId: 1);

        await service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true);

        var report = await context.DailyReports.SingleAsync();

        Assert.Equal(1, report.ChildId);
        Assert.Equal("teacher-user-id", report.CreatedByUserId);
        Assert.Equal(ChildMood.Happy, report.Mood);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotAllowParentToCreateReport()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        var service = new DailyReportService(context);
        var model = CreateModel(childId: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "parent-user-id", isAdmin: false, isTeacher: false));

        Assert.Empty(context.DailyReports);
    }

    [Fact]
    public async Task CreateAsync_ShouldBlockDuplicateReportForSameChildAndDate()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        context.DailyReports.Add(new DailyReport
        {
            Id = 1,
            ChildId = 1,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Calm,
            CreatedByUserId = "teacher-user-id"
        });
        await context.SaveChangesAsync();

        var service = new DailyReportService(context);
        var model = CreateModel(childId: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        Assert.Single(context.DailyReports);
    }

    [Fact]
    public async Task EditAsync_ShouldNotAllowTeacherToEditReportCreatedByAnotherTeacher()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        context.DailyReports.Add(new DailyReport
        {
            Id = 1,
            ChildId = 1,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Calm,
            CreatedByUserId = "other-teacher-user-id"
        });
        await context.SaveChangesAsync();

        var service = new DailyReportService(context);
        var model = new DailyReportEditViewModel
        {
            Id = 1,
            ChildFullName = "Ivan Ivanov",
            ReportDate = DateTime.Today,
            Mood = ChildMood.Happy
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EditAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        var report = await context.DailyReports.SingleAsync();

        Assert.Equal(ChildMood.Calm, report.Mood);
    }

    [Fact]
    public async Task EditAsync_ShouldAllowAdminToEditAnyReport()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        context.DailyReports.Add(new DailyReport
        {
            Id = 1,
            ChildId = 1,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Calm,
            CreatedByUserId = "teacher-user-id"
        });
        await context.SaveChangesAsync();

        var service = new DailyReportService(context);
        var model = new DailyReportEditViewModel
        {
            Id = 1,
            ChildFullName = "Ivan Ivanov",
            ReportDate = DateTime.Today,
            Mood = ChildMood.Happy,
            TeacherNote = "Updated by admin."
        };

        await service.EditAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var report = await context.DailyReports.SingleAsync();

        Assert.Equal(ChildMood.Happy, report.Mood);
        Assert.Equal("Updated by admin.", report.TeacherNote);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotAllowTeacherToDeleteReportCreatedByAnotherTeacher()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsAndChildrenAsync(context);

        context.DailyReports.Add(new DailyReport
        {
            Id = 1,
            ChildId = 1,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Calm,
            CreatedByUserId = "other-teacher-user-id"
        });
        await context.SaveChangesAsync();

        var service = new DailyReportService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(1, "teacher-user-id", isAdmin: false, isTeacher: true));

        var report = await context.DailyReports.SingleAsync();

        Assert.False(report.IsDeleted);
    }

    [Fact]
    public async Task CanAccessAsync_ShouldAllowParentToAccessReportForOwnChild()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.DailyReports.Add(new DailyReport
        {
            Id = 1,
            ChildId = 1,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Happy,
            CreatedByUserId = "teacher-user-id"
        });
        await context.SaveChangesAsync();

        var service = new DailyReportService(context);

        var canAccess = await service.CanAccessAsync(1, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.True(canAccess);
    }

    [Fact]
    public async Task CanAccessAsync_ShouldNotAllowParentToAccessReportForAnotherParentsChild()
    {
        await using var context = CreateContext();
        await SeedTwoParentsAndChildrenAsync(context);

        context.DailyReports.Add(new DailyReport
        {
            Id = 1,
            ChildId = 2,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Happy,
            CreatedByUserId = "teacher-user-id"
        });
        await context.SaveChangesAsync();

        var service = new DailyReportService(context);

        var canAccess = await service.CanAccessAsync(1, "first-parent-user-id", isAdmin: false, isTeacher: false);

        Assert.False(canAccess);
    }

    private static DailyReportCreateViewModel CreateModel(int childId)
    {
        return new DailyReportCreateViewModel
        {
            ChildId = childId,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Happy,
            Meals = "Lunch",
            Sleep = "One hour",
            Activities = "Drawing",
            TeacherNote = "Good day."
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

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "teacher-user-id",
                UserName = "teacher@kiddocare.com",
                Email = "teacher@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "other-teacher-user-id",
                UserName = "other.teacher@kiddocare.com",
                Email = "other.teacher@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "admin-user-id",
                UserName = "admin@kiddocare.com",
                Email = "admin@kiddocare.com"
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

        context.Users.Add(new IdentityUser
        {
            Id = "parent-user-id",
            UserName = "parent@kiddocare.com",
            Email = "parent@kiddocare.com"
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
}
