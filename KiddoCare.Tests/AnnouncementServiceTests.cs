using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Announcements;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class AnnouncementServiceTests
{
    [Fact]
    public async Task CanAccessAnnouncementAsync_ShouldNotAllowTeacherToAccessOtherGroupAnnouncement()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);

        var canAccess = await service.CanAccessAnnouncementAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.False(canAccess);
    }

    [Fact]
    public async Task CanAccessAnnouncementAsync_ShouldAllowTeacherToAccessOwnGroupAndAllGroupsAnnouncements()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);

        var canAccessOwnGroup = await service.CanAccessAnnouncementAsync(1, "teacher-user-id", isAdmin: false, isTeacher: true);
        var canAccessAllGroups = await service.CanAccessAnnouncementAsync(3, "teacher-user-id", isAdmin: false, isTeacher: true);
        var canAccessPrivateOwnGroup = await service.CanAccessAnnouncementAsync(4, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.True(canAccessOwnGroup);
        Assert.True(canAccessAllGroups);
        Assert.True(canAccessPrivateOwnGroup);
    }

    [Fact]
    public async Task CanAccessAnnouncementAsync_ShouldAllowParentToAccessOnlyPublicAnnouncementsForOwnChildGroupOrAllGroups()
    {
        await using var context = CreateContext();
        await SeedParentChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);

        var canAccessOwnGroupPublic = await service.CanAccessAnnouncementAsync(1, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessOtherGroupPublic = await service.CanAccessAnnouncementAsync(2, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessAllGroupsPublic = await service.CanAccessAnnouncementAsync(3, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessOwnGroupPrivate = await service.CanAccessAnnouncementAsync(4, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.True(canAccessOwnGroupPublic);
        Assert.False(canAccessOtherGroupPublic);
        Assert.True(canAccessAllGroupsPublic);
        Assert.False(canAccessOwnGroupPrivate);
    }

    [Fact]
    public async Task CreateAsync_ShouldForceTeacherAnnouncementToOwnGroupAndPublic()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);
        var model = CreateModel(groupId: 2, isPublic: false);

        await service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true);

        var createdAnnouncement = await context.Announcements
            .OrderByDescending(a => a.Id)
            .FirstAsync();

        Assert.Equal(1, createdAnnouncement.GroupId);
        Assert.True(createdAnnouncement.IsPublic);
    }

    [Fact]
    public async Task EditAsync_ShouldNotAllowTeacherToEditOtherGroupAnnouncement()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);
        var model = new AnnouncementEditViewModel
        {
            Id = 2,
            Title = "Updated announcement",
            Content = "Updated content",
            GroupId = 2,
            IsPublic = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EditAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        var announcement = await context.Announcements.FindAsync(2);

        Assert.Equal("Other group announcement", announcement!.Title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotAllowTeacherToDeleteOtherGroupAnnouncement()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true));

        var announcement = await context.Announcements.FindAsync(2);

        Assert.False(announcement!.IsDeleted);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyTeacherRelevantAnnouncements()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndAnnouncementsAsync(context);

        var service = new AnnouncementService(context);

        var result = (await service.GetAllAsync("teacher-user-id", isAdmin: false, isTeacher: true, searchTerm: null, page: 1, pageSize: 15)).Announcements.ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a.Title == "Own group announcement");
        Assert.Contains(result, a => a.Title == "All groups announcement");
        Assert.Contains(result, a => a.Title == "Private own group announcement");
        Assert.DoesNotContain(result, a => a.Title == "Other group announcement");
    }

    private static AnnouncementCreateViewModel CreateModel(int? groupId, bool isPublic)
    {
        return new AnnouncementCreateViewModel
        {
            Title = "New announcement",
            Content = "Announcement content",
            GroupId = groupId,
            IsPublic = isPublic
        };
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedTeacherGroupsChildrenAndAnnouncementsAsync(ApplicationDbContext context)
    {
        SeedGroups(context);

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

        SeedAnnouncements(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedParentChildrenAndAnnouncementsAsync(ApplicationDbContext context)
    {
        SeedGroups(context);

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

        SeedAnnouncements(context);

        await context.SaveChangesAsync();
    }

    private static void SeedGroups(ApplicationDbContext context)
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
    }

    private static void SeedAnnouncements(ApplicationDbContext context)
    {
        context.Announcements.AddRange(
            new Announcement
            {
                Id = 1,
                Title = "Own group announcement",
                Content = "Own group announcement content",
                GroupId = 1,
                IsPublic = true
            },
            new Announcement
            {
                Id = 2,
                Title = "Other group announcement",
                Content = "Other group announcement content",
                GroupId = 2,
                IsPublic = true
            },
            new Announcement
            {
                Id = 3,
                Title = "All groups announcement",
                Content = "All groups announcement content",
                GroupId = null,
                IsPublic = true
            },
            new Announcement
            {
                Id = 4,
                Title = "Private own group announcement",
                Content = "Private own group announcement content",
                GroupId = 1,
                IsPublic = false
            });
    }
}
