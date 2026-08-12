using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ShouldReturnAdminCountsAndRecentItems()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new DashboardService(context);

        var model = await service.GetDashboardAsync();

        Assert.Equal(2, model.GroupsCount);
        Assert.Equal(3, model.ChildrenCount);
        Assert.Equal(1, model.PresentTodayCount);
        Assert.Equal(1, model.AbsentTodayCount);
        Assert.Equal(1, model.SickTodayCount);
        Assert.Equal(2, model.PendingAbsenceRequestsCount);
        Assert.Equal(2, model.PendingConsentRequestsCount);
        Assert.Equal(2, model.PendingChildDocumentsCount);
        Assert.Contains(model.UpcomingEvents, e => e.Title == "Own group public event");
        Assert.Contains(model.RecentAnnouncements, a => a.Title == "Private own group announcement");
        Assert.Contains(model.RecentDocuments, d => d.Title == "Parent document");
    }

    [Fact]
    public async Task GetParentDashboardAsync_ShouldReturnOnlyParentChildrenAndRelevantPublicContent()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new DashboardService(context);

        var model = await service.GetParentDashboardAsync("parent-user-id");
        var children = model.Children.ToList();
        var events = model.UpcomingEvents.ToList();
        var announcements = model.RecentAnnouncements.ToList();

        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(children, c => c.FullName == "Georgi Georgiev");
        Assert.DoesNotContain(children, c => c.FullName == "Maria Petrova");
        Assert.Contains(events, e => e.Title == "Own group public event");
        Assert.Contains(events, e => e.Title == "All groups public event");
        Assert.DoesNotContain(events, e => e.Title == "Other group public event");
        Assert.DoesNotContain(events, e => e.Title == "Own group private event");
        Assert.Contains(announcements, a => a.Title == "Own group public announcement");
        Assert.Contains(announcements, a => a.Title == "All groups public announcement");
        Assert.DoesNotContain(announcements, a => a.Title == "Other group public announcement");
        Assert.DoesNotContain(announcements, a => a.Title == "Private own group announcement");
        Assert.Equal(1, model.PendingConsentRequestsCount);
        Assert.Equal(1, model.PendingChildDocumentsCount);
        Assert.Single(model.RecentAbsenceRequests);
        Assert.Single(model.RecentConsentRequests);
        Assert.Single(model.RecentDocuments);
    }

    [Fact]
    public async Task GetTeacherDashboardAsync_ShouldReturnOnlyTeacherGroupData()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new DashboardService(context);

        var model = await service.GetTeacherDashboardAsync("teacher-user-id");

        Assert.NotNull(model);
        Assert.Equal("Sunshine", model!.GroupName);
        Assert.Equal(2, model.ChildrenCount);
        Assert.Equal(1, model.PresentTodayCount);
        Assert.Equal(0, model.AbsentTodayCount);
        Assert.Equal(1, model.SickTodayCount);
        Assert.Equal(1, model.ChildrenWithMedicalRecordsCount);
        Assert.Equal(1, model.ChildrenWithAllergiesCount);
        Assert.Equal(1, model.PendingAbsenceRequestsCount);
        Assert.Equal(1, model.PendingConsentRequestsCount);
        Assert.Equal(1, model.PendingChildDocumentsCount);
        Assert.Contains(model.UpcomingEvents, e => e.Title == "Own group public event");
        Assert.Contains(model.UpcomingEvents, e => e.Title == "All groups public event");
        Assert.DoesNotContain(model.UpcomingEvents, e => e.Title == "Other group public event");
        Assert.Contains(model.RecentAnnouncements, a => a.Title == "Private own group announcement");
        Assert.DoesNotContain(model.RecentAnnouncements, a => a.Title == "Other group public announcement");
        Assert.Single(model.RecentDailyReports);
        Assert.Single(model.RecentDocuments);
    }

    [Fact]
    public async Task GetTeacherDashboardAsync_ShouldReturnNullWhenTeacherProfileDoesNotExist()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new DashboardService(context);

        var model = await service.GetTeacherDashboardAsync("missing-teacher-user-id");

        Assert.Null(model);
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
                Id = "teacher-user-id",
                UserName = "teacher@kiddocare.com",
                Email = "teacher@kiddocare.com"
            },
            new IdentityUser
            {
                Id = "other-teacher-user-id",
                UserName = "other-teacher@kiddocare.com",
                Email = "other-teacher@kiddocare.com"
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
            },
            new KindergartenGroup
            {
                Id = 3,
                Name = "Deleted Group",
                IsDeleted = true
            });

        context.TeacherProfiles.AddRange(
            new TeacherProfile
            {
                Id = 1,
                UserId = "teacher-user-id",
                FullName = "Teacher One",
                GroupId = 1
            },
            new TeacherProfile
            {
                Id = 2,
                UserId = "other-teacher-user-id",
                FullName = "Teacher Two",
                GroupId = 2
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

        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                Id = 1,
                ChildId = 1,
                Date = DateTime.Today,
                Status = AttendanceStatus.Present
            },
            new AttendanceRecord
            {
                Id = 2,
                ChildId = 2,
                Date = DateTime.Today,
                Status = AttendanceStatus.Absent
            },
            new AttendanceRecord
            {
                Id = 3,
                ChildId = 3,
                Date = DateTime.Today,
                Status = AttendanceStatus.Sick
            });

        context.Events.AddRange(
            new Event
            {
                Id = 1,
                Title = "Own group public event",
                StartDateTime = DateTime.Now.AddDays(1),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = true
            },
            new Event
            {
                Id = 2,
                Title = "Other group public event",
                StartDateTime = DateTime.Now.AddDays(2),
                Type = EventType.General,
                GroupId = 2,
                IsPublic = true
            },
            new Event
            {
                Id = 3,
                Title = "All groups public event",
                StartDateTime = DateTime.Now.AddDays(3),
                Type = EventType.General,
                GroupId = null,
                IsPublic = true
            },
            new Event
            {
                Id = 4,
                Title = "Own group private event",
                StartDateTime = DateTime.Now.AddDays(4),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = false
            },
            new Event
            {
                Id = 5,
                Title = "Past event",
                StartDateTime = DateTime.Now.AddDays(-1),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = true
            });

        context.Announcements.AddRange(
            new Announcement
            {
                Id = 1,
                Title = "Own group public announcement",
                Content = "Own group public announcement content",
                GroupId = 1,
                IsPublic = true,
                PublishedOn = DateTime.UtcNow.AddMinutes(-1)
            },
            new Announcement
            {
                Id = 2,
                Title = "Other group public announcement",
                Content = "Other group public announcement content",
                GroupId = 2,
                IsPublic = true,
                PublishedOn = DateTime.UtcNow.AddMinutes(-2)
            },
            new Announcement
            {
                Id = 3,
                Title = "All groups public announcement",
                Content = "All groups public announcement content",
                GroupId = null,
                IsPublic = true,
                PublishedOn = DateTime.UtcNow.AddMinutes(-3)
            },
            new Announcement
            {
                Id = 4,
                Title = "Private own group announcement",
                Content = "Private own group announcement content",
                GroupId = 1,
                IsPublic = false,
                PublishedOn = DateTime.UtcNow
            });

        context.MedicalRecords.Add(new MedicalRecord
        {
            Id = 1,
            ChildId = 1,
            Allergies = "Peanuts",
            ChronicConditions = "Asthma"
        });

        context.DailyReports.AddRange(
            new DailyReport
            {
                Id = 1,
                ChildId = 1,
                ReportDate = DateTime.Today,
                Mood = ChildMood.Happy,
                MealRating = 4,
                SleepRating = 3,
                ActivityRating = 5,
                CreatedByUserId = "teacher-user-id"
            },
            new DailyReport
            {
                Id = 2,
                ChildId = 2,
                ReportDate = DateTime.Today,
                Mood = ChildMood.Calm,
                MealRating = 3,
                SleepRating = 3,
                ActivityRating = 4,
                CreatedByUserId = "other-teacher-user-id"
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
                RequestedByUserId = "parent-user-id"
            },
            new AbsenceRequest
            {
                Id = 2,
                ChildId = 2,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(1),
                Reason = AbsenceReason.Vacation,
                Status = RequestStatus.Pending,
                RequestedByUserId = "other-parent-user-id"
            },
            new AbsenceRequest
            {
                Id = 3,
                ChildId = 3,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(2),
                Reason = AbsenceReason.Other,
                Status = RequestStatus.Rejected,
                RequestedByUserId = "parent-user-id"
            });

        context.ConsentRequests.AddRange(
            new ConsentRequest
            {
                Id = 1,
                ChildId = 1,
                Title = "Photo permission",
                Type = ConsentRequestType.PhotoPermission,
                Status = RequestStatus.Pending,
                CreatedByUserId = "teacher-user-id"
            },
            new ConsentRequest
            {
                Id = 2,
                ChildId = 2,
                Title = "Trip permission",
                Type = ConsentRequestType.FieldTrip,
                Status = RequestStatus.Pending,
                CreatedByUserId = "other-teacher-user-id"
            });

        context.ChildDocuments.AddRange(
            new ChildDocument
            {
                Id = 1,
                ChildId = 1,
                Type = ChildDocumentType.MedicalNote,
                Title = "Parent document",
                FileUrl = "/App_Data/uploads/child-documents/parent.pdf",
                Status = RequestStatus.Pending,
                UploadedByUserId = "parent-user-id"
            },
            new ChildDocument
            {
                Id = 2,
                ChildId = 2,
                Type = ChildDocumentType.BirthCertificate,
                Title = "Other parent document",
                FileUrl = "/App_Data/uploads/child-documents/other-parent.pdf",
                Status = RequestStatus.Pending,
                UploadedByUserId = "other-parent-user-id"
            });

        await context.SaveChangesAsync();
    }
}
