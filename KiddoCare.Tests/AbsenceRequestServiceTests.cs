using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.AbsenceRequests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class AbsenceRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldNotAllowTeacherToCreateNoticeForChildFromAnotherGroup()
    {
        await using var context = CreateContext();
        await SeedGroupsAndTeacherAsync(context);

        var service = new AbsenceRequestService(context);
        var model = new AbsenceRequestCreateViewModel
        {
            ChildId = 2,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(1),
            Reason = AbsenceReason.Vacation
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        Assert.Empty(context.AbsenceRequests);
    }

    [Fact]
    public async Task CreateAsync_ShouldConfirmTeacherNoticeForOwnGroupAndCreateAttendance()
    {
        await using var context = CreateContext();
        await SeedGroupsAndTeacherAsync(context);

        var service = new AbsenceRequestService(context);
        var model = new AbsenceRequestCreateViewModel
        {
            ChildId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2),
            Reason = AbsenceReason.Sick
        };

        await service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true);

        var notice = await context.AbsenceRequests.SingleAsync();
        var attendanceRecords = await context.AttendanceRecords
            .OrderBy(a => a.Date)
            .ToListAsync();

        Assert.Equal(RequestStatus.Approved, notice.Status);
        Assert.Equal("teacher-user-id", notice.ReviewedByUserId);
        Assert.NotNull(notice.ReviewedOn);
        Assert.Equal("Confirmed on creation.", notice.ReviewNote);
        Assert.Equal(2, attendanceRecords.Count);
        Assert.All(attendanceRecords, record =>
        {
            Assert.Equal(1, record.ChildId);
            Assert.Equal(AttendanceStatus.Sick, record.Status);
            Assert.Equal("Created from confirmed absence notice.", record.Note);
        });
    }

    [Fact]
    public async Task CreateAsync_ShouldKeepParentNoticePending()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        var service = new AbsenceRequestService(context);
        var model = new AbsenceRequestCreateViewModel
        {
            ChildId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(1),
            Reason = AbsenceReason.Vacation
        };

        await service.CreateAsync(model, "parent-user-id", isAdmin: false, isTeacher: false);

        var notice = await context.AbsenceRequests.SingleAsync();

        Assert.Equal(RequestStatus.Pending, notice.Status);
        Assert.Null(notice.ReviewedByUserId);
        Assert.Null(notice.ReviewedOn);
        Assert.Empty(context.AttendanceRecords);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectOverlappingActiveNotice()
    {
        await using var context = CreateContext();
        await SeedParentAndChildAsync(context);

        context.AbsenceRequests.Add(new AbsenceRequest
        {
            ChildId = 1,
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(4),
            Reason = AbsenceReason.Vacation,
            Status = RequestStatus.Pending,
            RequestedByUserId = "parent-user-id"
        });
        await context.SaveChangesAsync();

        var service = new AbsenceRequestService(context);
        var model = new AbsenceRequestCreateViewModel
        {
            ChildId = 1,
            StartDate = DateTime.Today.AddDays(3),
            EndDate = DateTime.Today.AddDays(5),
            Reason = AbsenceReason.Sick
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "parent-user-id", isAdmin: false, isTeacher: false));

        Assert.Single(context.AbsenceRequests);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedGroupsAndTeacherAsync(ApplicationDbContext context)
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
}
