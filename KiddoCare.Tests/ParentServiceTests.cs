using KiddoCare.Common;
using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Parents;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Tests;

public class ParentServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveParentsOrderedByName()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());

        var result = (await service.GetAllAsync(searchTerm: null, page: 1, pageSize: 15)).Parents.ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Parent One", result[0].FullName);
        Assert.Equal("Parent Two", result[1].FullName);
        Assert.Equal(2, result[0].ChildrenCount);
        Assert.DoesNotContain(result, p => p.FullName == "Deleted Parent");
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnParentWithOnlyActiveChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());

        var model = await service.GetDetailsAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Parent One", model!.FullName);
        Assert.Equal("parent@kiddocare.com", model.Email);
        Assert.Equal(2, model.Children.Count());
        Assert.Contains(model.Children, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(model.Children, c => c.FullName == "Georgi Georgiev");
        Assert.DoesNotContain(model.Children, c => c.FullName == "Deleted Child");
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnNullForDeletedParent()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());

        var model = await service.GetDetailsAsync(3);

        Assert.Null(model);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateIdentityUserWithParentRoleAndParentProfile()
    {
        await using var context = CreateContext();
        await SeedRolesAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());
        var model = new ParentCreateViewModel
        {
            Email = "new-parent@kiddocare.com",
            FullName = "New Parent",
            PhoneNumber = "0888333333"
        };

        await service.CreateAsync(model);

        var user = await userManager.FindByEmailAsync("new-parent@kiddocare.com");
        var parent = await context.ParentProfiles
            .FirstOrDefaultAsync(p => p.FullName == "New Parent" && !p.IsDeleted);

        Assert.NotNull(user);
        Assert.NotNull(parent);
        Assert.Equal(user!.Id, parent!.UserId);
        Assert.Equal("0888333333", user.PhoneNumber);
        Assert.True(user.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(user, Parent));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowWhenEmailAlreadyExists()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());
        var model = new ParentCreateViewModel
        {
            Email = "parent@kiddocare.com",
            FullName = "Duplicate Parent",
            PhoneNumber = "0888444444"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(model));
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNullForDeletedParent()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());

        var model = await service.GetForEditAsync(3);

        Assert.Null(model);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateParentProfileAndIdentityUserPhoneNumber()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());
        var model = new ParentEditViewModel
        {
            Id = 1,
            FullName = "Updated Parent",
            PhoneNumber = "0899999999"
        };

        await service.EditAsync(model);

        var parent = await context.ParentProfiles
            .Include(p => p.User)
            .FirstAsync(p => p.Id == 1);

        Assert.Equal("Updated Parent", parent.FullName);
        Assert.Equal("0899999999", parent.PhoneNumber);
        Assert.Equal("0899999999", parent.User.PhoneNumber);
    }

    [Fact]
    public async Task EditAsync_ShouldThrowForDeletedParent()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());
        var model = new ParentEditViewModel
        {
            Id = 3,
            FullName = "Updated Deleted Parent"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EditAsync(model));
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnActiveChildrenCount()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());

        var model = await service.GetForDeleteAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Parent One", model!.FullName);
        Assert.Equal("parent@kiddocare.com", model.Email);
        Assert.Equal(2, model.ChildrenCount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteParentProfile()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);
        using var userManager = CreateUserManager(context);

        var service = new ParentService(context, userManager, CreateConfiguration());

        await service.DeleteAsync(1);

        var parent = await context.ParentProfiles.FindAsync(1);

        Assert.True(parent!.IsDeleted);
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
                [UserPasswordConfigurationKeys.DefaultParentPassword] = "TestParentPassword123!"
            })
            .Build();
    }

    private static async Task SeedDataAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "parent-user-id",
                UserName = "parent@kiddocare.com",
                NormalizedUserName = "PARENT@KIDDOCARE.COM",
                Email = "parent@kiddocare.com",
                NormalizedEmail = "PARENT@KIDDOCARE.COM",
                EmailConfirmed = true,
                PhoneNumber = "0888111111"
            },
            new IdentityUser
            {
                Id = "other-parent-user-id",
                UserName = "other-parent@kiddocare.com",
                NormalizedUserName = "OTHER-PARENT@KIDDOCARE.COM",
                Email = "other-parent@kiddocare.com",
                NormalizedEmail = "OTHER-PARENT@KIDDOCARE.COM",
                EmailConfirmed = true,
                PhoneNumber = "0888222222"
            },
            new IdentityUser
            {
                Id = "deleted-parent-user-id",
                UserName = "deleted-parent@kiddocare.com",
                NormalizedUserName = "DELETED-PARENT@KIDDOCARE.COM",
                Email = "deleted-parent@kiddocare.com",
                NormalizedEmail = "DELETED-PARENT@KIDDOCARE.COM",
                EmailConfirmed = true,
                PhoneNumber = "0888333333"
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
                PhoneNumber = "0888333333",
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

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        context.Roles.Add(new IdentityRole
        {
            Id = "parent-role-id",
            Name = Parent,
            NormalizedName = Parent.ToUpperInvariant()
        });

        await context.SaveChangesAsync();
    }
}

