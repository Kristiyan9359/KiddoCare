using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Events;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class EventService : IEventService
{
    private readonly ApplicationDbContext context;

    public EventService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<EventIndexViewModel>> GetAllAsync(string userId, bool isAdminOrTeacher)
    {
        var query = context.Events
         .Where(e => !e.IsDeleted)
         .AsQueryable();

        if (!isAdminOrTeacher)
        {
            var parentGroupIds = await context.Children
                .Where(c =>
                    !c.IsDeleted &&
                    c.Parent != null &&
                    c.Parent.UserId == userId)
                .Select(c => c.GroupId)
                .Distinct()
                .ToListAsync();

            query = query.Where(e =>
                e.IsPublic &&
                (e.GroupId == null || parentGroupIds.Contains(e.GroupId.Value)));
        }

        return await query
            .OrderBy(e => e.StartDateTime)
            .Select(e => new EventIndexViewModel
            {
                Id = e.Id,
                Title = e.Title,
                StartDateTime = e.StartDateTime,
                Type = e.Type,
                Location = e.Location,
                GroupName = e.Group == null ? "All groups" : e.Group.Name
            })
            .ToListAsync();
    }

    public async Task<EventDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.Events
            .Where(e => e.Id == id && !e.IsDeleted)
            .Select(e => new EventDetailsViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                Type = e.Type,
                Location = e.Location,
                GroupName = e.Group == null ? "All groups" : e.Group.Name,
                IsPublic = e.IsPublic
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanAccessEventAsync(int eventId, string userId, bool isAdminOrTeacher)
    {
        if (isAdminOrTeacher)
        {
            return await context.Events
                .AnyAsync(e => e.Id == eventId && !e.IsDeleted);
        }

        var parentGroupIds = await context.Children
            .Where(c =>
                !c.IsDeleted &&
                c.Parent != null &&
                c.Parent.UserId == userId)
            .Select(c => c.GroupId)
            .Distinct()
            .ToListAsync();

        return await context.Events
            .AnyAsync(e =>
                e.Id == eventId &&
                !e.IsDeleted &&
                e.IsPublic &&
                (e.GroupId == null || parentGroupIds.Contains(e.GroupId.Value)));
    }

    public async Task<EventCreateViewModel> GetCreateModelAsync()
    {
        return new EventCreateViewModel
        {
            Groups = await GetGroupSelectListAsync()
        };
    }

    public async Task<EventEditViewModel?> GetForEditAsync(int id)
    {
        var model = await context.Events
            .Where(e => e.Id == id && !e.IsDeleted)
            .Select(e => new EventEditViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                Type = e.Type,
                Location = e.Location,
                GroupId = e.GroupId,
                IsPublic = e.IsPublic
            })
            .FirstOrDefaultAsync();

        if (model == null)
        {
            return null;
        }

        model.Groups = await GetGroupSelectListAsync();

        return model;
    }

    public async Task CreateAsync(EventCreateViewModel model)
    {
        var eventEntity = new Event
        {
            Title = model.Title,
            Description = model.Description,
            StartDateTime = model.StartDateTime,
            EndDateTime = model.EndDateTime,
            Type = model.Type,
            Location = model.Location,
            GroupId = model.GroupId,
            IsPublic = model.IsPublic
        };

        await context.Events.AddAsync(eventEntity);
        await context.SaveChangesAsync();
    }

    public async Task EditAsync(EventEditViewModel model)
    {
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.Id == model.Id && !e.IsDeleted);

        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found.");
        }

        eventEntity.Title = model.Title;
        eventEntity.Description = model.Description;
        eventEntity.StartDateTime = model.StartDateTime;
        eventEntity.EndDateTime = model.EndDateTime;
        eventEntity.Type = model.Type;
        eventEntity.Location = model.Location;
        eventEntity.GroupId = model.GroupId;
        eventEntity.IsPublic = model.IsPublic;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found.");
        }

        eventEntity.IsDeleted = true;

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
}