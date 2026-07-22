using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Groups;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class GroupService : IGroupService
{
    private readonly ApplicationDbContext context;

    public GroupService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<GroupIndexViewModel>> GetAllAsync()
    {
        return await context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Name)
            .Select(g => new GroupIndexViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description
            })
            .ToListAsync();
    }

    public async Task<GroupEditViewModel?> GetForEditAsync(int id)
    {
        return await context.KindergartenGroups
            .Where(g => g.Id == id && !g.IsDeleted)
            .Select(g => new GroupEditViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(GroupCreateViewModel model)
    {
        var group = new KindergartenGroup
        {
            Name = model.Name,
            Description = model.Description
        };

        await context.KindergartenGroups.AddAsync(group);
        await context.SaveChangesAsync();
    }

    public async Task EditAsync(GroupEditViewModel model)
    {
        var group = await context.KindergartenGroups
            .FirstOrDefaultAsync(g => g.Id == model.Id && !g.IsDeleted);

        if (group == null)
        {
            throw new InvalidOperationException("Group not found.");
        }

        group.Name = model.Name;
        group.Description = model.Description;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var group = await context.KindergartenGroups
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

        if (group == null)
        {
            throw new InvalidOperationException("Group not found.");
        }

        group.IsDeleted = true;

        await context.SaveChangesAsync();
    }
}