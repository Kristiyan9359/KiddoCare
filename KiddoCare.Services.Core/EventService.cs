using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
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

    public async Task<EventListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, int page, int pageSize)
    {
        var query = context.Events
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        int? teacherGroupId = null;

        if (isTeacher && !isAdmin)
        {
            teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new EventListViewModel
                {
                    SearchTerm = searchTerm,
                    Page = 1,
                    PageSize = pageSize
                };
            }

            query = query.Where(e =>
                e.GroupId == null ||
                e.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            var matchingTypes = Enum.GetValues<EventType>()
                .Where(t => t.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            query = query.Where(e =>
                e.Title.Contains(searchTerm) ||
                (e.Location != null && e.Location.Contains(searchTerm)) ||
                (e.Group != null && e.Group.Name.Contains(searchTerm)) ||
                matchingTypes.Contains(e.Type));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalEvents = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalEvents / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var events = await query
            .OrderBy(e => e.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventIndexViewModel
            {
                Id = e.Id,
                Title = e.Title,
                StartDateTime = e.StartDateTime,
                Type = e.Type,
                Location = e.Location,
                GroupName = e.Group == null ? "All groups" : e.Group.Name,
                CanManage = isAdmin || (teacherGroupId.HasValue && e.GroupId == teacherGroupId.Value)
            })
            .ToListAsync();

        return new EventListViewModel
        {
            Events = events,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalEvents = totalEvents
        };
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        var query = context.Events
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<string>();
            }

            query = query.Where(e =>
                e.GroupId == null ||
                e.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
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

        var matchingTypes = Enum.GetValues<EventType>()
            .Where(t => t.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Where(e =>
                e.Title.Contains(term) ||
                (e.Location != null && e.Location.Contains(term)) ||
                (e.Group != null && e.Group.Name.Contains(term)) ||
                matchingTypes.Contains(e.Type))
            .OrderBy(e => e.StartDateTime)
            .Select(e => e.Title)
            .Distinct()
            .Take(8)
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

    public async Task<bool> CanAccessEventAsync(int eventId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Events
                .AnyAsync(e => e.Id == eventId && !e.IsDeleted);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await context.Events.AnyAsync(e =>
                       e.Id == eventId &&
                       !e.IsDeleted &&
                       (e.GroupId == null || e.GroupId == teacherGroupId.Value));
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

    public async Task<EventCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new EventCreateViewModel
        {
            Groups = await GetGroupSelectListAsync(userId, isAdmin, isTeacher)
        };
    }

    public async Task<EventEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var eventEntity = await context.Events
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

        if (eventEntity == null)
        {
            return null;
        }

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null || eventEntity.GroupId != teacherGroupId.Value)
            {
                return null;
            }
        }

        eventEntity.Groups = await GetGroupSelectListAsync(userId, isAdmin, isTeacher);

        return eventEntity;
    }

    public async Task CreateAsync(EventCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                throw new InvalidOperationException("Teacher group not found.");
            }

            model.GroupId = teacherGroupId.Value;
            model.IsPublic = true;
        }

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

    public async Task EditAsync(EventEditViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.Id == model.Id && !e.IsDeleted);

        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found.");
        }

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null || eventEntity.GroupId != teacherGroupId.Value)
            {
                throw new InvalidOperationException("Event not found.");
            }

            model.GroupId = teacherGroupId.Value;
            model.IsPublic = true;
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

    public async Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found.");
        }

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null || eventEntity.GroupId != teacherGroupId.Value)
            {
                throw new InvalidOperationException("Event not found.");
            }
        }

        eventEntity.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetGroupSelectListAsync(string userId, bool isAdmin, bool isTeacher)
    {
        var query = context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<SelectListItem>();
            }

            query = query.Where(g => g.Id == teacherGroupId.Value);
        }

        return await query
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();
    }

    private async Task<int?> GetTeacherGroupIdAsync(string userId)
    {
        return await context.TeacherProfiles
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .Select(t => (int?)t.GroupId)
            .FirstOrDefaultAsync();
    }
}
