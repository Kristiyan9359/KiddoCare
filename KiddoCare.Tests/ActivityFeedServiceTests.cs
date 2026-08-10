using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class ActivityFeedServiceTests
{
    [Fact]
    public async Task GetChildFeedAsync_ShouldReturnParentOwnChildFeedWithRelevantPublicItems()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ActivityFeedService(context);

        var model = await service.GetChildFeedAsync(1, "parent-user-id", isAdmin: false, isTeacher: false);
        var items = model!.Items.ToList();

        Assert.NotNull(model);
        Assert.Equal("Ivan Ivanov", model.ChildFullName);
        Assert.Contains(items, i => i.Type == "Attendance");
        Assert.Contains(items, i => i.Type == "Daily Report");
        Assert.Contains(items, i => i.Type == "Absence Notice" && i.Description == "Sick - Pending");
        Assert.Contains(items, i => i.Type == "Consent Request");
        Assert.Contains(items, i => i.Type == "Event" && i.Title == "Own group public event");
        Assert.Contains(items, i => i.Type == "Event" && i.Title == "All groups public event");
        Assert.Contains(items, i => i.Type == "Announcement" && i.Title == "Own group public announcement");
        Assert.Contains(items, i => i.Type == "Announcement" && i.Title == "All groups public announcement");
        Assert.DoesNotContain(items, i => i.Title == "Other group public event");
        Assert.DoesNotContain(items, i => i.Title == "Own group private event");
        Assert.DoesNotContain(items, i => i.Title == "Other group public announcement");
        Assert.DoesNotContain(items, i => i.Title == "Private own group announcement");
        Assert.DoesNotContain(items, i => i.Description == "Other - Rejected");
    }

    [Fact]
    public async Task GetChildFeedAsync_ShouldAllowTeacherToAccessOwnGroupChildAndStaffPrivateAnnouncements()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ActivityFeedService(context);

        var model = await service.GetChildFeedAsync(1, "teacher-user-id", isAdmin: false, isTeacher: true);
        var items = model!.Items.ToList();

        Assert.NotNull(model);
        Assert.Equal("Ivan Ivanov", model.ChildFullName);
        Assert.Contains(items, i => i.Type == "Announcement" && i.Title == "Private own group announcement");
        Assert.DoesNotContain(items, i => i.Title == "Other group public announcement");
    }

    [Fact]
    public async Task GetChildFeedAsync_ShouldNotAllowTeacherToAccessOtherGroupChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ActivityFeedService(context);

        var model = await service.GetChildFeedAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.Null(model);
    }

    [Fact]
    public async Task GetChildFeedAsync_ShouldNotAllowParentToAccessOtherParentChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ActivityFeedService(context);

        var model = await service.GetChildFeedAsync(2, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.Null(model);
    }

    [Fact]
    public async Task GetChildFeedAsync_ShouldReturnNullForDeletedChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ActivityFeedService(context);

        var model = await service.GetChildFeedAsync(3, "admin-user-id", isAdmin: true, isTeacher: false);

        Assert.Null(model);
    }

    [Fact]
    public async Task GetChildFeedAsync_ShouldOrderItemsByDateDescending()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new ActivityFeedService(context);

        var model = await service.GetChildFeedAsync(1, "admin-user-id", isAdmin: true, isTeacher: false);
        var items = model!.Items.ToList();

        Assert.Equal(items.OrderByDescending(i => i.Date).Select(i => i.Title), items.Select(i => i.Title));
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
            });

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
                FullName = "Parent One"
            },
            new ParentProfile
            {
                Id = 2,
                UserId = "other-parent-user-id",
                FullName = "Parent Two"
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
                DateOfBirth = DateTime.Today.AddYears(-3),
                GroupId = 2,
                ParentId = 2
            },
            new Child
            {
                Id = 3,
                FirstName = "Deleted",
                LastName = "Child",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1,
                ParentId = 1,
                IsDeleted = true
            });

        context.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = 1,
            ChildId = 1,
            Date = DateTime.Today,
            Status = AttendanceStatus.Present
        });

        context.DailyReports.AddRange(
            new DailyReport
            {
                Id = 1,
                ChildId = 1,
                ReportDate = DateTime.Today.AddHours(1),
                Mood = ChildMood.Happy,
                CreatedByUserId = "teacher-user-id"
            },
            new DailyReport
            {
                Id = 2,
                ChildId = 1,
                ReportDate = DateTime.Today.AddHours(2),
                Mood = ChildMood.Calm,
                CreatedByUserId = "teacher-user-id",
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
                Status = RequestStatus.Pending,
                RequestedByUserId = "parent-user-id",
                RequestedOn = DateTime.Today.AddHours(2)
            },
            new AbsenceRequest
            {
                Id = 2,
                ChildId = 1,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(2),
                Reason = AbsenceReason.Other,
                Status = RequestStatus.Rejected,
                RequestedByUserId = "parent-user-id",
                RequestedOn = DateTime.Today.AddHours(3)
            });

        context.ConsentRequests.Add(new ConsentRequest
        {
            Id = 1,
            ChildId = 1,
            Title = "Photo permission",
            Type = ConsentRequestType.PhotoPermission,
            Status = RequestStatus.Pending,
            CreatedByUserId = "teacher-user-id",
            CreatedOn = DateTime.Today.AddHours(4)
        });

        context.Events.AddRange(
            new Event
            {
                Id = 1,
                Title = "Own group public event",
                StartDateTime = DateTime.Today.AddDays(1),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = true
            },
            new Event
            {
                Id = 2,
                Title = "Other group public event",
                StartDateTime = DateTime.Today.AddDays(2),
                Type = EventType.General,
                GroupId = 2,
                IsPublic = true
            },
            new Event
            {
                Id = 3,
                Title = "All groups public event",
                StartDateTime = DateTime.Today.AddDays(3),
                Type = EventType.General,
                GroupId = null,
                IsPublic = true
            },
            new Event
            {
                Id = 4,
                Title = "Own group private event",
                StartDateTime = DateTime.Today.AddDays(4),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = false
            },
            new Event
            {
                Id = 5,
                Title = "Deleted event",
                StartDateTime = DateTime.Today.AddDays(5),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = true,
                IsDeleted = true
            });

        context.Announcements.AddRange(
            new Announcement
            {
                Id = 1,
                Title = "Own group public announcement",
                Content = "Own group public announcement content",
                GroupId = 1,
                IsPublic = true,
                PublishedOn = DateTime.Today.AddHours(5)
            },
            new Announcement
            {
                Id = 2,
                Title = "Other group public announcement",
                Content = "Other group public announcement content",
                GroupId = 2,
                IsPublic = true,
                PublishedOn = DateTime.Today.AddHours(6)
            },
            new Announcement
            {
                Id = 3,
                Title = "All groups public announcement",
                Content = "All groups public announcement content",
                GroupId = null,
                IsPublic = true,
                PublishedOn = DateTime.Today.AddHours(7)
            },
            new Announcement
            {
                Id = 4,
                Title = "Private own group announcement",
                Content = "Private own group announcement content",
                GroupId = 1,
                IsPublic = false,
                PublishedOn = DateTime.Today.AddHours(8)
            },
            new Announcement
            {
                Id = 5,
                Title = "Deleted announcement",
                Content = "Deleted announcement content",
                GroupId = 1,
                IsPublic = true,
                PublishedOn = DateTime.Today.AddHours(9),
                IsDeleted = true
            });

        await context.SaveChangesAsync();
    }
}
