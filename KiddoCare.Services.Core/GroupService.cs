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

    public async Task<GroupListViewModel> GetAllAsync(string? searchTerm, int page, int pageSize)
    {
        var query = context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(g =>
                g.Name.Contains(searchTerm) ||
                (g.Description != null && g.Description.Contains(searchTerm)));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalGroups = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalGroups / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var groups = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GroupIndexViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description
            })
            .ToListAsync();

        return new GroupListViewModel
        {
            Groups = groups,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalGroups = totalGroups
        };
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

    public async Task<GroupDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.KindergartenGroups
            .Where(g => g.Id == id && !g.IsDeleted)
            .Select(g => new GroupDetailsViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Children = g.Children
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.FirstName)
                    .ThenBy(c => c.LastName)
                    .Select(c => new GroupChildViewModel
                    {
                        Id = c.Id,
                        FullName = c.FirstName + " " + c.LastName,
                        DateOfBirth = c.DateOfBirth
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var group = await context.KindergartenGroups
            .Include(g => g.Children)
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

        if (group == null)
        {
            throw new InvalidOperationException("Group not found.");
        }

        if (group.Children.Any(c => !c.IsDeleted))
        {
            throw new InvalidOperationException("Cannot delete a group that has active children.");
        }

        group.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    public async Task<GroupDeleteViewModel?> GetForDeleteAsync(int id)
    {
        return await context.KindergartenGroups
            .Where(g => g.Id == id && !g.IsDeleted)
            .Select(g => new GroupDeleteViewModel
            {
                Id = g.Id,
                Name = g.Name,
                ChildrenCount = g.Children.Count(c => !c.IsDeleted)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        return await context.KindergartenGroups
            .Where(g =>
                !g.IsDeleted &&
                (g.Name.Contains(term) ||
                 (g.Description != null && g.Description.Contains(term))))
            .OrderBy(g => g.Name)
            .Select(g => g.Name)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }
}