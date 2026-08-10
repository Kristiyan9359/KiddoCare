using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core;
using KiddoCare.ViewModels.Groups;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Tests;

public class GroupServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveGroupsOrderedByName()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Empty Group", result[0].Name);
        Assert.Equal("Moonlight", result[1].Name);
        Assert.Equal("Sunshine", result[2].Name);
        Assert.DoesNotContain(result, g => g.Name == "Deleted Group");
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnGroupWithOnlyActiveChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);

        var model = await service.GetDetailsAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Sunshine", model!.Name);
        Assert.Equal(2, model.Children.Count());
        Assert.Contains(model.Children, c => c.FullName == "Ivan Ivanov");
        Assert.Contains(model.Children, c => c.FullName == "Georgi Georgiev");
        Assert.DoesNotContain(model.Children, c => c.FullName == "Deleted Child");
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNullForDeletedGroup()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);

        var model = await service.GetForEditAsync(4);

        Assert.Null(model);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateGroup()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);
        var model = new GroupCreateViewModel
        {
            Name = "Stars",
            Description = "Stars group"
        };

        await service.CreateAsync(model);

        var group = await context.KindergartenGroups
            .FirstOrDefaultAsync(g => g.Name == "Stars" && !g.IsDeleted);

        Assert.NotNull(group);
        Assert.Equal("Stars group", group!.Description);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateGroup()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);
        var model = new GroupEditViewModel
        {
            Id = 1,
            Name = "Updated Sunshine",
            Description = "Updated description"
        };

        await service.EditAsync(model);

        var group = await context.KindergartenGroups.FindAsync(1);

        Assert.Equal("Updated Sunshine", group!.Name);
        Assert.Equal("Updated description", group.Description);
    }

    [Fact]
    public async Task EditAsync_ShouldThrowForDeletedGroup()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);
        var model = new GroupEditViewModel
        {
            Id = 4,
            Name = "Updated Deleted Group"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EditAsync(model));
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotAllowDeletingGroupWithActiveChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1));

        var group = await context.KindergartenGroups.FindAsync(1);

        Assert.False(group!.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteGroupWithoutActiveChildren()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);

        await service.DeleteAsync(3);

        var group = await context.KindergartenGroups.FindAsync(3);

        Assert.True(group!.IsDeleted);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnActiveChildrenCount()
    {
        await using var context = CreateContext();
        await SeedDataAsync(context);

        var service = new GroupService(context);

        var model = await service.GetForDeleteAsync(1);

        Assert.NotNull(model);
        Assert.Equal("Sunshine", model!.Name);
        Assert.Equal(2, model.ChildrenCount);
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
                Name = "Sunshine",
                Description = "Sunshine group"
            },
            new KindergartenGroup
            {
                Id = 2,
                Name = "Moonlight",
                Description = "Moonlight group"
            },
            new KindergartenGroup
            {
                Id = 3,
                Name = "Empty Group",
                Description = "Group without active children"
            },
            new KindergartenGroup
            {
                Id = 4,
                Name = "Deleted Group",
                Description = "Deleted group",
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
            },
            new Child
            {
                Id = 4,
                FirstName = "Deleted",
                LastName = "Child",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1,
                IsDeleted = true
            });

        await context.SaveChangesAsync();
    }
}
