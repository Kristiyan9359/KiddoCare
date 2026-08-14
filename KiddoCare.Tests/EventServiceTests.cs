using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class EventServiceTests
{
    [Fact]
    public async Task CanAccessEventAsync_ShouldNotAllowTeacherToAccessOtherGroupEvent()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndEventsAsync(context);

        var service = new EventService(context);

        var canAccess = await service.CanAccessEventAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.False(canAccess);
    }

    [Fact]
    public async Task CanAccessEventAsync_ShouldAllowTeacherToAccessOwnGroupAndAllGroupsEvents()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndEventsAsync(context);

        var service = new EventService(context);

        var canAccessOwnGroup = await service.CanAccessEventAsync(1, "teacher-user-id", isAdmin: false, isTeacher: true);
        var canAccessAllGroups = await service.CanAccessEventAsync(3, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.True(canAccessOwnGroup);
        Assert.True(canAccessAllGroups);
    }

    [Fact]
    public async Task CanAccessEventAsync_ShouldAllowParentToAccessOnlyPublicEventsForOwnChildGroupOrAllGroups()
    {
        await using var context = CreateContext();
        await SeedParentChildrenAndEventsAsync(context);

        var service = new EventService(context);

        var canAccessOwnGroupPublic = await service.CanAccessEventAsync(1, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessOtherGroupPublic = await service.CanAccessEventAsync(2, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessAllGroupsPublic = await service.CanAccessEventAsync(3, "parent-user-id", isAdmin: false, isTeacher: false);
        var canAccessOwnGroupPrivate = await service.CanAccessEventAsync(4, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.True(canAccessOwnGroupPublic);
        Assert.False(canAccessOtherGroupPublic);
        Assert.True(canAccessAllGroupsPublic);
        Assert.False(canAccessOwnGroupPrivate);
    }

    [Fact]
    public async Task CreateAsync_ShouldForceTeacherEventToOwnGroupAndPublic()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndEventsAsync(context);

        var service = new EventService(context);
        var model = CreateModel(groupId: 2, isPublic: false);

        await service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true);

        var createdEvent = await context.Events
            .OrderByDescending(e => e.Id)
            .FirstAsync();

        Assert.Equal(1, createdEvent.GroupId);
        Assert.True(createdEvent.IsPublic);
    }

    [Fact]
    public async Task EditAsync_ShouldNotAllowTeacherToEditOtherGroupEvent()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndEventsAsync(context);

        var service = new EventService(context);
        var model = new EventEditViewModel
        {
            Id = 2,
            Title = "Updated event",
            StartDateTime = DateTime.Today.AddDays(5),
            Type = EventType.Trip,
            GroupId = 2,
            IsPublic = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EditAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        var eventEntity = await context.Events.FindAsync(2);

        Assert.Equal("Other group event", eventEntity!.Title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotAllowTeacherToDeleteOtherGroupEvent()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndEventsAsync(context);

        var service = new EventService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true));

        var eventEntity = await context.Events.FindAsync(2);

        Assert.False(eventEntity!.IsDeleted);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyTeacherRelevantEvents()
    {
        await using var context = CreateContext();
        await SeedTeacherGroupsChildrenAndEventsAsync(context);

        var service = new EventService(context);

        var result = (await service.GetAllAsync("teacher-user-id", isAdmin: false, isTeacher: true, searchTerm: null, page: 1, pageSize: 15)).Events.ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.Title == "Own group event");
        Assert.Contains(result, e => e.Title == "All groups event");
        Assert.Contains(result, e => e.Title == "Private own group event");
        Assert.DoesNotContain(result, e => e.Title == "Other group event");
    }

    private static EventCreateViewModel CreateModel(int? groupId, bool isPublic)
    {
        return new EventCreateViewModel
        {
            Title = "New event",
            Description = "Event description",
            StartDateTime = DateTime.Today.AddDays(7),
            Type = EventType.Celebration,
            Location = "Garden",
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

    private static async Task SeedTeacherGroupsChildrenAndEventsAsync(ApplicationDbContext context)
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

        SeedEvents(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedParentChildrenAndEventsAsync(ApplicationDbContext context)
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

        SeedEvents(context);

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

    private static void SeedEvents(ApplicationDbContext context)
    {
        context.Events.AddRange(
            new Event
            {
                Id = 1,
                Title = "Own group event",
                StartDateTime = DateTime.Today.AddDays(1),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = true
            },
            new Event
            {
                Id = 2,
                Title = "Other group event",
                StartDateTime = DateTime.Today.AddDays(2),
                Type = EventType.General,
                GroupId = 2,
                IsPublic = true
            },
            new Event
            {
                Id = 3,
                Title = "All groups event",
                StartDateTime = DateTime.Today.AddDays(3),
                Type = EventType.General,
                GroupId = null,
                IsPublic = true
            },
            new Event
            {
                Id = 4,
                Title = "Private own group event",
                StartDateTime = DateTime.Today.AddDays(4),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = false
            });
    }
}
