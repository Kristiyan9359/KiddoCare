using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Children;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class ChildService : IChildService
{
    private readonly ApplicationDbContext context;

    public ChildService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<ChildIndexViewModel>> GetAllAsync()
    {
        return await context.Children
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new ChildIndexViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                GroupName = c.Group.Name,
                PhotoUrl = c.PhotoUrl
            })
            .ToListAsync();
    }

    public async Task<ChildCreateViewModel> GetCreateModelAsync()
    {
        return new ChildCreateViewModel
        {
            Groups = await GetGroupSelectListAsync()
        };
    }

    public async Task<ChildEditViewModel?> GetForEditAsync(int id)
    {
        var child = await context.Children
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new ChildEditViewModel
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Gender = c.Gender,
                DateOfBirth = c.DateOfBirth,
                Allergies = c.Allergies,
                AdditionalNotes = c.AdditionalNotes,
                GroupId = c.GroupId,
                PhotoUrl = c.PhotoUrl
            })
            .FirstOrDefaultAsync();

        if (child == null)
        {
            return null;
        }

        child.Groups = await GetGroupSelectListAsync();

        return child;
    }

    public async Task CreateAsync(ChildCreateViewModel model)
    {
        var child = new Child
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            Allergies = model.Allergies,
            AdditionalNotes = model.AdditionalNotes,
            GroupId = model.GroupId,
            PhotoUrl = model.PhotoUrl
        };

        await context.Children.AddAsync(child);
        await context.SaveChangesAsync();
    }

    public async Task EditAsync(ChildEditViewModel model)
    {
        var child = await context.Children
            .FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        child.FirstName = model.FirstName;
        child.LastName = model.LastName;
        child.Gender = model.Gender;
        child.DateOfBirth = model.DateOfBirth;
        child.Allergies = model.Allergies;
        child.AdditionalNotes = model.AdditionalNotes;
        child.GroupId = model.GroupId;
        child.PhotoUrl = model.PhotoUrl;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var child = await context.Children
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        child.IsDeleted = true;

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

    public async Task<ChildDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.Children
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new ChildDetailsViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                GroupName = c.Group.Name,
                Allergies = c.Allergies,
                AdditionalNotes = c.AdditionalNotes,
                PhotoUrl = c.PhotoUrl
            })
            .FirstOrDefaultAsync();
    }
}