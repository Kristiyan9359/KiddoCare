using KiddoCare.Common;
using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Teachers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Tests;

public class TeacherServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveTeachersOrderedByName()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Teacher One", result[0].FullName);
        Assert.Equal("Teacher Two", result[1].FullName);
        Assert.Equal("Sunshine", result[0].GroupName);
        Assert.DoesNotContain(result, t => t.FullName == "Deleted Teacher");
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnTeacherDetails()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var model = await service.GetDetailsAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Teacher One", model!.FullName);
        Assert.Equal("teacher@kiddocare.com", model.Email);
        Assert.Equal("0888111111", model.PhoneNumber);
        Assert.Equal("Sunshine", model.GroupName);
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnNullForDeletedTeacher()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var model = await service.GetDetailsAsync(3);

        Assert.Null(model);
    }

    [Fact]
    public async Task GetCreateModelAsync_ShouldReturnOnlyActiveGroups()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var model = await service.GetCreateModelAsync();
        var groups = model.Groups.ToList();

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, g => g.Text == "Moonlight");
        Assert.Contains(groups, g => g.Text == "Sunshine");
        Assert.DoesNotContain(groups, g => g.Text == "Deleted Group");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateIdentityUserWithTeacherRoleAndTeacherProfile()
    {
        await using var context = CreateContext();
        await SeedRolesAndGroupsAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());
        var model = new TeacherCreateViewModel
        {
            Email = "new-teacher@kiddocare.com",
            FullName = "New Teacher",
            PhoneNumber = "0888333333",
            GroupId = 1
        };

        await service.CreateAsync(model);

        var user = await userManager.FindByEmailAsync("new-teacher@kiddocare.com");
        var teacher = await context.TeacherProfiles
            .FirstOrDefaultAsync(t => t.FullName == "New Teacher" && !t.IsDeleted);

        Assert.NotNull(user);
        Assert.NotNull(teacher);
        Assert.Equal(user!.Id, teacher!.UserId);
        Assert.Equal(1, teacher.GroupId);
        Assert.Equal("0888333333", user.PhoneNumber);
        Assert.True(user.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(user, Teacher));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowWhenEmailAlreadyExists()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());
        var model = new TeacherCreateViewModel
        {
            Email = "teacher@kiddocare.com",
            FullName = "Duplicate Teacher",
            PhoneNumber = "0888444444",
            GroupId = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(model));
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnTeacherWithActiveGroups()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var model = await service.GetForEditAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Teacher One", model!.FullName);
        Assert.Equal(1, model.GroupId);
        Assert.Equal(2, model.Groups.Count());
        Assert.DoesNotContain(model.Groups, g => g.Text == "Deleted Group");
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNullForDeletedTeacher()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var model = await service.GetForEditAsync(3);

        Assert.Null(model);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateTeacherProfileAndIdentityUserPhoneNumber()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());
        var model = new TeacherEditViewModel
        {
            Id = 1,
            FullName = "Updated Teacher",
            PhoneNumber = "0899999999",
            GroupId = 2
        };

        await service.EditAsync(model);

        var teacher = await context.TeacherProfiles
            .Include(t => t.User)
            .FirstAsync(t => t.Id == 1);

        Assert.Equal("Updated Teacher", teacher.FullName);
        Assert.Equal("0899999999", teacher.PhoneNumber);
        Assert.Equal("0899999999", teacher.User.PhoneNumber);
        Assert.Equal(2, teacher.GroupId);
    }

    [Fact]
    public async Task EditAsync_ShouldThrowForDeletedTeacher()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());
        var model = new TeacherEditViewModel
        {
            Id = 3,
            FullName = "Updated Deleted Teacher",
            GroupId = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EditAsync(model));
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnTeacherDeleteModel()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        var model = await service.GetForDeleteAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Teacher One", model!.FullName);
        Assert.Equal("teacher@kiddocare.com", model.Email);
        Assert.Equal("Sunshine", model.GroupName);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteTeacherProfile()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new TeacherService(context, userManager, CreateConfiguration());

        await service.DeleteAsync(1);

        var teacher = await context.TeacherProfiles.FindAsync(1);

        Assert.True(teacher!.IsDeleted);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static UserManager<IdentityUser> CreateUserManager(ApplicationDbContext context)
    {
        var userStore = new UserStore<IdentityUser>(context);
        var services = new ServiceCollection().BuildServiceProvider();

        return new UserManager<IdentityUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<IdentityUser>(),
            new List<IUserValidator<IdentityUser>> { new UserValidator<IdentityUser>() },
            new List<IPasswordValidator<IdentityUser>> { new PasswordValidator<IdentityUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services,
            NullLogger<UserManager<IdentityUser>>.Instance);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [UserPasswordConfigurationKeys.DefaultTeacherPassword] = "TestTeacherPassword123!"
            })
            .Build();
    }

    private static async Task SeedDataAsync(ApplicationDbContext context)
    {
        await SeedRolesAndGroupsAsync(context);

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "teacher-user-id",
                UserName = "teacher@kiddocare.com",
                NormalizedUserName = "TEACHER@KIDDOCARE.COM",
                Email = "teacher@kiddocare.com",
                NormalizedEmail = "TEACHER@KIDDOCARE.COM",
                EmailConfirmed = true,
                PhoneNumber = "0888111111"
            },
            new IdentityUser
            {
                Id = "other-teacher-user-id",
                UserName = "other-teacher@kiddocare.com",
                NormalizedUserName = "OTHER-TEACHER@KIDDOCARE.COM",
                Email = "other-teacher@kiddocare.com",
                NormalizedEmail = "OTHER-TEACHER@KIDDOCARE.COM",
                EmailConfirmed = true,
                PhoneNumber = "0888222222"
            },
            new IdentityUser
            {
                Id = "deleted-teacher-user-id",
                UserName = "deleted-teacher@kiddocare.com",
                NormalizedUserName = "DELETED-TEACHER@KIDDOCARE.COM",
                Email = "deleted-teacher@kiddocare.com",
                NormalizedEmail = "DELETED-TEACHER@KIDDOCARE.COM",
                EmailConfirmed = true,
                PhoneNumber = "0888333333"
            });

        context.TeacherProfiles.AddRange(
            new TeacherProfile
            {
                Id = 1,
                UserId = "teacher-user-id",
                FullName = "Teacher One",
                PhoneNumber = "0888111111",
                GroupId = 1
            },
            new TeacherProfile
            {
                Id = 2,
                UserId = "other-teacher-user-id",
                FullName = "Teacher Two",
                PhoneNumber = "0888222222",
                GroupId = 2
            },
            new TeacherProfile
            {
                Id = 3,
                UserId = "deleted-teacher-user-id",
                FullName = "Deleted Teacher",
                PhoneNumber = "0888333333",
                GroupId = 1,
                IsDeleted = true
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAndGroupsAsync(ApplicationDbContext context)
    {
        context.Roles.Add(new IdentityRole
        {
            Id = "teacher-role-id",
            Name = Teacher,
            NormalizedName = Teacher.ToUpperInvariant()
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

        await context.SaveChangesAsync();
    }
}

