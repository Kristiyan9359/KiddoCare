using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.MedicalRecords;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class MedicalRecordServiceTests
{
    [Fact]
    public async Task GetDetailsAsync_ShouldAllowTeacherToAccessOwnGroupChildMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);

        var model = await service.GetDetailsAsync(1, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.NotNull(model);
        Assert.Equal("Ivan Ivanov", model!.ChildFullName);
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldNotAllowTeacherToAccessOtherGroupChildMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);

        var model = await service.GetDetailsAsync(2, "teacher-user-id", isAdmin: false, isTeacher: true);

        Assert.Null(model);
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldAllowParentToAccessOwnChildMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);

        var model = await service.GetDetailsAsync(1, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.NotNull(model);
        Assert.Equal("Ivan Ivanov", model!.ChildFullName);
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldNotAllowParentToAccessOtherParentChildMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);

        var model = await service.GetDetailsAsync(2, "parent-user-id", isAdmin: false, isTeacher: false);

        Assert.Null(model);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateMedicalRecordWhenUserIsAdmin()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);
        var model = CreateModel(childId: 3);

        await service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var medicalRecord = await context.MedicalRecords
            .FirstOrDefaultAsync(m => m.ChildId == 3 && !m.IsDeleted);

        Assert.NotNull(medicalRecord);
        Assert.Equal("No allergies", medicalRecord!.Allergies);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotAllowTeacherToCreateMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);
        var model = CreateModel(childId: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));
    }

    [Fact]
    public async Task CreateAsync_ShouldNotAllowDuplicateActiveMedicalRecordForChild()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);
        var model = CreateModel(childId: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false));
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowNewMedicalRecordAfterPreviousOneIsDeleted()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var oldMedicalRecord = await context.MedicalRecords.FindAsync(1);
        oldMedicalRecord!.IsDeleted = true;
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var model = CreateModel(childId: 1);

        await service.CreateAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var activeRecordsCount = await context.MedicalRecords
            .CountAsync(m => m.ChildId == 1 && !m.IsDeleted);

        Assert.Equal(1, activeRecordsCount);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateMedicalRecordWhenUserIsAdmin()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);
        var model = new MedicalRecordEditViewModel
        {
            Id = 1,
            ChildFullName = "Ivan Ivanov",
            Allergies = "Updated allergies",
            ChronicConditions = "Updated conditions",
            DoctorName = "Dr. Updated",
            DoctorPhone = "0888888888",
            EmergencyContactName = "Updated Contact",
            EmergencyContactPhone = "0877777777",
            Notes = "Updated notes",
            ChildId = 1
        };

        await service.EditAsync(model, "admin-user-id", isAdmin: true, isTeacher: false);

        var medicalRecord = await context.MedicalRecords.FindAsync(1);

        Assert.Equal("Updated allergies", medicalRecord!.Allergies);
        Assert.Equal("Dr. Updated", medicalRecord.DoctorName);
    }

    [Fact]
    public async Task EditAsync_ShouldNotAllowTeacherToEditMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);
        var model = new MedicalRecordEditViewModel
        {
            Id = 1,
            ChildFullName = "Ivan Ivanov",
            Allergies = "Updated allergies",
            ChildId = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EditAsync(model, "teacher-user-id", isAdmin: false, isTeacher: true));

        var medicalRecord = await context.MedicalRecords.FindAsync(1);

        Assert.Equal("Peanuts", medicalRecord!.Allergies);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteMedicalRecordWhenUserIsAdmin()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);

        await service.DeleteAsync(1, "admin-user-id", isAdmin: true, isTeacher: false);

        var medicalRecord = await context.MedicalRecords.FindAsync(1);

        Assert.True(medicalRecord!.IsDeleted);
    }

    [Fact]
    public async Task GetCreateModelAsync_ShouldReturnOnlyChildrenWithoutActiveMedicalRecord()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new MedicalRecordService(context);

        var model = await service.GetCreateModelAsync("admin-user-id", isAdmin: true, isTeacher: false);
        var childIds = model.Children.Select(c => c.Value).ToList();

        Assert.DoesNotContain("1", childIds);
        Assert.DoesNotContain("2", childIds);
        Assert.Contains("3", childIds);
    }

    private static MedicalRecordCreateViewModel CreateModel(int childId)
    {
        return new MedicalRecordCreateViewModel
        {
            ChildId = childId,
            Allergies = "No allergies",
            ChronicConditions = "None",
            DoctorName = "Dr. Petrov",
            DoctorPhone = "0888123456",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "0877123456",
            Notes = "Notes"
        };
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
                FirstName = "Georgi",
                LastName = "Georgiev",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-5),
                GroupId = 1,
                ParentId = 1
            });

        context.MedicalRecords.AddRange(
            new MedicalRecord
            {
                Id = 1,
                ChildId = 1,
                Allergies = "Peanuts",
                ChronicConditions = "Asthma",
                DoctorName = "Dr. Ivanov",
                DoctorPhone = "0888111111",
                EmergencyContactName = "Parent One",
                EmergencyContactPhone = "0877111111",
                Notes = "Important notes"
            },
            new MedicalRecord
            {
                Id = 2,
                ChildId = 2,
                Allergies = "None",
                ChronicConditions = "None",
                DoctorName = "Dr. Petrova",
                DoctorPhone = "0888222222",
                EmergencyContactName = "Parent Two",
                EmergencyContactPhone = "0877222222",
                Notes = "Other notes"
            });

        await context.SaveChangesAsync();
    }
}
