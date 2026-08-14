using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Announcements;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class AnnouncementService : IAnnouncementService
{
    private readonly ApplicationDbContext context;

    public AnnouncementService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<AnnouncementListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, int page, int pageSize)
    {
        var query = context.Announcements
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        int? teacherGroupId = null;

        if (isTeacher && !isAdmin)
        {
            teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new AnnouncementListViewModel
                {
                    SearchTerm = searchTerm,
                    Page = 1,
                    PageSize = pageSize
                };
            }

            query = query.Where(a =>
                a.GroupId == null ||
                a.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            var parentGroupIds = await GetParentGroupIdsAsync(userId);

            query = query.Where(a =>
                a.IsPublic &&
                (a.GroupId == null || parentGroupIds.Contains(a.GroupId.Value)));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(a =>
                a.Title.Contains(searchTerm) ||
                a.Content.Contains(searchTerm) ||
                (a.Group != null && a.Group.Name.Contains(searchTerm)));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalAnnouncements = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalAnnouncements / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var announcements = await query
            .OrderByDescending(a => a.PublishedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AnnouncementIndexViewModel
            {
                Id = a.Id,
                Title = a.Title,
                ContentPreview = a.Content.Length > 120
                    ? a.Content.Substring(0, 120) + "..."
                    : a.Content,
                GroupName = a.Group == null ? "All groups" : a.Group.Name,
                PublishedOn = a.PublishedOn,
                CanManage = isAdmin || (teacherGroupId.HasValue && a.GroupId == teacherGroupId.Value)
            })
            .ToListAsync();

        return new AnnouncementListViewModel
        {
            Announcements = announcements,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalAnnouncements = totalAnnouncements
        };
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        var query = context.Announcements
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<string>();
            }

            query = query.Where(a =>
                a.GroupId == null ||
                a.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            var parentGroupIds = await GetParentGroupIdsAsync(userId);

            query = query.Where(a =>
                a.IsPublic &&
                (a.GroupId == null || parentGroupIds.Contains(a.GroupId.Value)));
        }

        return await query
            .Where(a =>
                a.Title.Contains(term) ||
                a.Content.Contains(term) ||
                (a.Group != null && a.Group.Name.Contains(term)))
            .OrderByDescending(a => a.PublishedOn)
            .Select(a => a.Title)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }

    public async Task<AnnouncementDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.Announcements
            .Where(a => a.Id == id && !a.IsDeleted)
            .Select(a => new AnnouncementDetailsViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                GroupName = a.Group == null ? "All groups" : a.Group.Name,
                PublishedOn = a.PublishedOn,
                IsPublic = a.IsPublic
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanAccessAnnouncementAsync(int announcementId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Announcements
                .AnyAsync(a => a.Id == announcementId && !a.IsDeleted);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return false;
            }

            return await context.Announcements
                .AnyAsync(a =>
                    a.Id == announcementId &&
                    !a.IsDeleted &&
                    (a.GroupId == null || a.GroupId == teacherGroupId.Value));
        }

        var parentGroupIds = await GetParentGroupIdsAsync(userId);

        return await context.Announcements
            .AnyAsync(a =>
                a.Id == announcementId &&
                !a.IsDeleted &&
                a.IsPublic &&
                (a.GroupId == null || parentGroupIds.Contains(a.GroupId.Value)));
    }

    public async Task<AnnouncementCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new AnnouncementCreateViewModel
        {
            Groups = await GetGroupSelectListAsync(userId, isAdmin, isTeacher)
        };
    }

    public async Task CreateAsync(AnnouncementCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
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

        var announcement = new Announcement
        {
            Title = model.Title,
            Content = model.Content,
            GroupId = model.GroupId,
            IsPublic = model.IsPublic
        };

        await context.Announcements.AddAsync(announcement);
        await context.SaveChangesAsync();
    }

    public async Task<AnnouncementEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var announcement = await context.Announcements
            .Where(a => a.Id == id && !a.IsDeleted)
            .Select(a => new AnnouncementEditViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                GroupId = a.GroupId,
                IsPublic = a.IsPublic
            })
            .FirstOrDefaultAsync();

        if (announcement == null)
        {
            return null;
        }

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null || announcement.GroupId != teacherGroupId.Value)
            {
                return null;
            }
        }

        announcement.Groups = await GetGroupSelectListAsync(userId, isAdmin, isTeacher);

        return announcement;
    }

    public async Task EditAsync(AnnouncementEditViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == model.Id && !a.IsDeleted);

        if (announcement == null)
        {
            throw new InvalidOperationException("Announcement not found.");
        }

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null || announcement.GroupId != teacherGroupId.Value)
            {
                throw new InvalidOperationException("Announcement not found.");
            }

            model.GroupId = teacherGroupId.Value;
            model.IsPublic = true;
        }

        announcement.Title = model.Title;
        announcement.Content = model.Content;
        announcement.GroupId = model.GroupId;
        announcement.IsPublic = model.IsPublic;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (announcement == null)
        {
            throw new InvalidOperationException("Announcement not found.");
        }

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null || announcement.GroupId != teacherGroupId.Value)
            {
                throw new InvalidOperationException("Announcement not found.");
            }
        }

        announcement.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    private async Task<int?> GetTeacherGroupIdAsync(string userId)
    {
        return await context.TeacherProfiles
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .Select(t => (int?)t.GroupId)
            .FirstOrDefaultAsync();
    }

    private async Task<List<int>> GetParentGroupIdsAsync(string userId)
    {
        return await context.Children
            .Where(c =>
                !c.IsDeleted &&
                c.Parent != null &&
                c.Parent.UserId == userId)
            .Select(c => c.GroupId)
            .Distinct()
            .ToListAsync();
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
}
