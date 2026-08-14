using KiddoCare.Common;
using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Teachers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KiddoCare.Services.Core;

public class TeacherService : ITeacherService
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<IdentityUser> userManager;
    private readonly IConfiguration configuration;

    public TeacherService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        this.context = context;
        this.userManager = userManager;
        this.configuration = configuration;
    }

    public async Task<TeacherListViewModel> GetAllAsync(string? searchTerm, int page, int pageSize)
    {
        var query = context.TeacherProfiles
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(t =>
                t.FullName.Contains(searchTerm) ||
                t.User.Email!.Contains(searchTerm) ||
                t.Group.Name.Contains(searchTerm) ||
                (t.PhoneNumber != null && t.PhoneNumber.Contains(searchTerm)));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalTeachers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalTeachers / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var teachers = await query
            .OrderBy(t => t.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeacherIndexViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.User.Email!,
                PhoneNumber = t.PhoneNumber,
                GroupName = t.Group.Name
            })
            .ToListAsync();

        return new TeacherListViewModel
        {
            Teachers = teachers,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalTeachers = totalTeachers
        };
    }

    public async Task<TeacherDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.TeacherProfiles
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new TeacherDetailsViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.User.Email!,
                PhoneNumber = t.PhoneNumber,
                GroupName = t.Group.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TeacherCreateViewModel> GetCreateModelAsync()
    {
        return new TeacherCreateViewModel
        {
            Groups = await GetGroupSelectListAsync()
        };
    }

    public async Task CreateAsync(TeacherCreateViewModel model)
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

        var defaultTeacherPassword = configuration[UserPasswordConfigurationKeys.DefaultTeacherPassword];

        if (string.IsNullOrWhiteSpace(defaultTeacherPassword))
        {
            throw new InvalidOperationException(
                $"Default teacher password is not configured. Set '{UserPasswordConfigurationKeys.DefaultTeacherPassword}' in user secrets.");
        }

        var result = await userManager.CreateAsync(user, defaultTeacherPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Teacher user creation failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, RoleConstants.Teacher);

        var teacher = new TeacherProfile
        {
            UserId = user.Id,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            GroupId = model.GroupId
        };

        await context.TeacherProfiles.AddAsync(teacher);
        await context.SaveChangesAsync();
    }

    public async Task<TeacherEditViewModel?> GetForEditAsync(int id)
    {
        var model = await context.TeacherProfiles
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new TeacherEditViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                PhoneNumber = t.PhoneNumber,
                GroupId = t.GroupId
            })
            .FirstOrDefaultAsync();

        if (model == null)
        {
            return null;
        }

        model.Groups = await GetGroupSelectListAsync();

        return model;
    }

    public async Task EditAsync(TeacherEditViewModel model)
    {
        var teacher = await context.TeacherProfiles
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == model.Id && !t.IsDeleted);

        if (teacher == null)
        {
            throw new InvalidOperationException("Teacher profile not found.");
        }

        teacher.FullName = model.FullName;
        teacher.PhoneNumber = model.PhoneNumber;
        teacher.GroupId = model.GroupId;
        teacher.User.PhoneNumber = model.PhoneNumber;

        await context.SaveChangesAsync();
    }

    public async Task<TeacherDeleteViewModel?> GetForDeleteAsync(int id)
    {
        return await context.TeacherProfiles
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new TeacherDeleteViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.User.Email!,
                GroupName = t.Group.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var teacher = await context.TeacherProfiles
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (teacher == null)
        {
            throw new InvalidOperationException("Teacher profile not found.");
        }

        teacher.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetGroupSelectListAsync()
    {
        return await context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        return await context.TeacherProfiles
            .Where(t =>
                !t.IsDeleted &&
                (t.FullName.Contains(term) ||
                 t.User.Email!.Contains(term) ||
                 t.Group.Name.Contains(term) ||
                 (t.PhoneNumber != null && t.PhoneNumber.Contains(term))))
            .OrderBy(t => t.FullName)
            .Select(t => t.FullName)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }
}
