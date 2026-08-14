using KiddoCare.Common;
using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Parents;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KiddoCare.Services.Core;

public class ParentService : IParentService
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<IdentityUser> userManager;
    private readonly IConfiguration configuration;

    public ParentService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        this.context = context;
        this.userManager = userManager;
        this.configuration = configuration;
    }

    public async Task<ParentListViewModel> GetAllAsync(string? searchTerm, int page, int pageSize)
    {
        var query = context.ParentProfiles
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(p =>
                p.FullName.Contains(searchTerm) ||
                p.User.Email!.Contains(searchTerm) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm)));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalParents = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalParents / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var parents = await query
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ParentIndexViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                Email = p.User.Email!,
                PhoneNumber = p.PhoneNumber,
                ChildrenCount = p.Children.Count(c => !c.IsDeleted)
            })
            .ToListAsync();

        return new ParentListViewModel
        {
            Parents = parents,
            Page = page,
            PageSize = pageSize,
            TotalParents = totalParents,
            SearchTerm = searchTerm
        };
    }

    public async Task<ParentDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.ParentProfiles
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ParentDetailsViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                Email = p.User.Email!,
                PhoneNumber = p.PhoneNumber,
                Children = p.Children
                  .Where(c => !c.IsDeleted)
                  .Select(c => new ParentChildViewModel
                  {
                      Id = c.Id,
                      FullName = c.FirstName + " " + c.LastName
                  })
                  .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public Task<ParentCreateViewModel> GetCreateModelAsync()
    {
        return Task.FromResult(new ParentCreateViewModel());
    }

    public async Task CreateAsync(ParentCreateViewModel model)
    {
        var existingUser = await userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            PhoneNumber = model.PhoneNumber
        };

        var defaultParentPassword = configuration[UserPasswordConfigurationKeys.DefaultParentPassword];

        if (string.IsNullOrWhiteSpace(defaultParentPassword))
        {
            throw new InvalidOperationException(
                $"Default parent password is not configured. Set '{UserPasswordConfigurationKeys.DefaultParentPassword}' in user secrets.");
        }

        var result = await userManager.CreateAsync(user, defaultParentPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Parent user creation failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, RoleConstants.Parent);

        var parent = new ParentProfile
        {
            UserId = user.Id,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber
        };

        await context.ParentProfiles.AddAsync(parent);
        await context.SaveChangesAsync();
    }

    public async Task<ParentEditViewModel?> GetForEditAsync(int id)
    {
        return await context.ParentProfiles
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ParentEditViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber
            })
            .FirstOrDefaultAsync();
    }

    public async Task EditAsync(ParentEditViewModel model)
    {
        var parent = await context.ParentProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);

        if (parent == null)
        {
            throw new InvalidOperationException("Parent profile not found.");
        }

        parent.FullName = model.FullName;
        parent.PhoneNumber = model.PhoneNumber;
        parent.User.PhoneNumber = model.PhoneNumber;

        await context.SaveChangesAsync();
    }

    public async Task<ParentDeleteViewModel?> GetForDeleteAsync(int id)
    {
        return await context.ParentProfiles
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ParentDeleteViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                Email = p.User.Email!,
                ChildrenCount = p.Children.Count(c => !c.IsDeleted)
            })
            .FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var parent = await context.ParentProfiles
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (parent == null)
        {
            throw new InvalidOperationException("Parent profile not found.");
        }

        parent.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        return await context.ParentProfiles
            .Where(p =>
                !p.IsDeleted &&
                (p.FullName.Contains(term) ||
                 p.User.Email!.Contains(term) ||
                 (p.PhoneNumber != null && p.PhoneNumber.Contains(term))))
            .OrderBy(p => p.FullName)
            .Select(p => p.FullName)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }
}
