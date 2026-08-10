using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Attendance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class AttendanceServiceTests
{
    [Fact]
    public async Task GetDailyAttendanceAsync_ShouldReturnOnlyTeacherGroupChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);

        var model = await service.GetDailyAttendanceAsync(
            DateTime.Today,
            groupId: 2,
            "teacher-user-id",
            isAdmin: false,
            isTeacher: true);

        Assert.Equal(1, model.GroupId);
        Assert.Equal(2, model.Children.Count);
        Assert.Contains(model.Children, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(model.Children, c => c.FullName == "Georgi Georgiev");
        Assert.DoesNotContain(model.Children, c => c.FullName == "Maria Petrova");
    }

    [Fact]
    public async Task GetDailyAttendanceAsync_ShouldUseExistingRecordStatusAndSummary()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);

        var model = await service.GetDailyAttendanceAsync(
            DateTime.Today,
            groupId: 1,
            "admin-user-id",
            isAdmin: true,
            isTeacher: false);

        var child = model.Children.First(c => c.ChildId == 1);

        Assert.Equal(AttendanceStatus.Sick, child.Status);
        Assert.Equal("Fever", child.Note);
        Assert.Equal(1, model.Summary.SickCount);
        Assert.Equal(1, model.Summary.PresentCount);
        Assert.Equal(2, model.Summary.TotalCount);
    }

    [Fact]
    public async Task SaveDailyAttendanceAsync_ShouldIgnoreOtherGroupChildrenWhenUserIsTeacher()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);
        var model = new AttendanceDailyViewModel
        {
            Date = DateTime.Today.AddDays(1),
            Children =
            {
                new AttendanceChildViewModel
                {
                    ChildId = 1,
                    Status = AttendanceStatus.Absent,
                    Note = "Own group child"
                },
                new AttendanceChildViewModel
                {
                    ChildId = 2,
                    Status = AttendanceStatus.Absent,
                    Note = "Other group child"
                }
            }
        };

        await service.SaveDailyAttendanceAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true);

        var ownGroupRecord = await context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.ChildId == 1 && a.Date == model.Date.Date);
        var otherGroupRecord = await context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.ChildId == 2 && a.Date == model.Date.Date);

        Assert.NotNull(ownGroupRecord);
        Assert.Null(otherGroupRecord);
    }

    [Fact]
    public async Task SaveDailyAttendanceAsync_ShouldCreateRecordsForAllChildrenWhenUserIsAdmin()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);
        var model = new AttendanceDailyViewModel
        {
            Date = DateTime.Today.AddDays(1),
            Children =
            {
                new AttendanceChildViewModel
                {
                    ChildId = 1,
                    Status = AttendanceStatus.Absent
                },
                new AttendanceChildViewModel
                {
                    ChildId = 2,
                    Status = AttendanceStatus.Late
                }
            }
        };

        await service.SaveDailyAttendanceAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var recordsCount = await context.AttendanceRecords
            .CountAsync(a => a.Date == model.Date.Date);

        Assert.Equal(2, recordsCount);
    }

    [Fact]
    public async Task SaveDailyAttendanceAsync_ShouldUpdateExistingRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);
        var model = new AttendanceDailyViewModel
        {
            Date = DateTime.Today,
            Children =
            {
                new AttendanceChildViewModel
                {
                    ChildId = 1,
                    Status = AttendanceStatus.Present,
                    Note = "Back again"
                }
            }
        };

        await service.SaveDailyAttendanceAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var record = await context.AttendanceRecords
            .FirstAsync(a => a.ChildId == 1 && a.Date == DateTime.Today);

        Assert.Equal(AttendanceStatus.Present, record.Status);
        Assert.Equal("Back again", record.Note);
    }

    [Fact]
    public async Task SaveDailyAttendanceAsync_ShouldThrowWhenTeacherHasNoGroup()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);
        var model = new AttendanceDailyViewModel
        {
            Date = DateTime.Today,
            Children =
            {
                new AttendanceChildViewModel
                {
                    ChildId = 1,
                    Status = AttendanceStatus.Absent
                }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveDailyAttendanceAsync(model, "teacher-without-profile-id", isAdmin: false, isTeacher: true));
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnOnlyTeacherGroupRecords()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);
        var filter = new AttendanceFilterViewModel
        {
            GroupId = 2
        };

        var model = await service.GetHistoryAsync(filter, "teacher-user-id", isAdmin: false, isTeacher: true);
        var records = model.Records.ToList();

        Assert.Equal(1, model.GroupId);
        Assert.Equal(2, records.Count);
        Assert.Contains(records, r => r.ChildName == "Ivan Ivanov");
        Assert.Contains(records, r => r.ChildName == "Georgi Georgiev");
        Assert.DoesNotContain(records, r => r.ChildName == "Maria Petrova");
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldApplyStatusAndDateFilters()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new AttendanceService(context);
        var filter = new AttendanceFilterViewModel
        {
            FromDate = DateTime.Today,
            ToDate = DateTime.Today,
            Status = AttendanceStatus.Sick
        };

        var model = await service.GetHistoryAsync(filter, "admin-user-id", isAdmin: true, isTeacher: false);
        var records = model.Records.ToList();

        Assert.Single(records);
        Assert.Equal("Ivan Ivanov", records[0].ChildName);
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
                DateOfBirth = DateTime.Today.AddYears(-3),
                GroupId = 2
            },
            new Child
            {
                Id = 3,
                FirstName = "Georgi",
                LastName = "Georgiev",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-5),
                GroupId = 1
            });

        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                Id = 1,
                ChildId = 1,
                Date = DateTime.Today,
                Status = AttendanceStatus.Sick,
                Note = "Fever"
            },
            new AttendanceRecord
            {
                Id = 2,
                ChildId = 2,
                Date = DateTime.Today,
                Status = AttendanceStatus.Absent,
                Note = "Vacation"
            },
            new AttendanceRecord
            {
                Id = 3,
                ChildId = 3,
                Date = DateTime.Today.AddDays(-1),
                Status = AttendanceStatus.Late,
                Note = "Traffic"
            });

        await context.SaveChangesAsync();
    }
}
