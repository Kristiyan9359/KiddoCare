using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ChildDocuments;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class ChildDocumentService : IChildDocumentService
{
    private readonly ApplicationDbContext context;

    public ChildDocumentService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<ChildDocumentListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, string? statusFilter, int page, int pageSize)
    {
        var query = context.ChildDocuments
            .Where(d => !d.IsDeleted && !d.Child.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new ChildDocumentListViewModel
                {
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    Page = 1,
                    PageSize = pageSize
                };
            }

            query = query.Where(d => d.Child.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(d =>
                d.Child.Parent != null &&
                !d.Child.Parent.IsDeleted &&
                d.Child.Parent.UserId == userId);
        }

        if (statusFilter == "pending")
        {
            query = query.Where(d => d.Status == RequestStatus.Pending);
        }
        else if (statusFilter == "approved")
        {
            query = query.Where(d => d.Status == RequestStatus.Approved);
        }
        else if (statusFilter == "rejected")
        {
            query = query.Where(d => d.Status == RequestStatus.Rejected);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            var matchingTypes = Enum.GetValues<ChildDocumentType>()
                .Where(t => t.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            query = query.Where(d =>
                d.Title.Contains(searchTerm) ||
                (d.Child.FirstName + " " + d.Child.LastName).Contains(searchTerm) ||
                d.Child.Group.Name.Contains(searchTerm) ||
                matchingTypes.Contains(d.Type));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalDocuments = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var documents = await query
            .OrderByDescending(d => d.UploadedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new ChildDocumentIndexViewModel
            {
                Id = d.Id,
                ChildFullName = d.Child.FirstName + " " + d.Child.LastName,
                GroupName = d.Child.Group.Name,
                Type = d.Type,
                Title = d.Title,
                Status = d.Status,
                UploadedOn = d.UploadedOn,
                CanReview = isAdmin && d.Status == RequestStatus.Pending
            })
            .ToListAsync();

        return new ChildDocumentListViewModel
        {
            Documents = documents,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            Page = page,
            PageSize = pageSize,
            TotalDocuments = totalDocuments
        };
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        var query = context.ChildDocuments
            .Where(d => !d.IsDeleted && !d.Child.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<string>();
            }

            query = query.Where(d => d.Child.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(d =>
                d.Child.Parent != null &&
                !d.Child.Parent.IsDeleted &&
                d.Child.Parent.UserId == userId);
        }

        var matchingTypes = Enum.GetValues<ChildDocumentType>()
            .Where(t => t.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Where(d =>
                d.Title.Contains(term) ||
                (d.Child.FirstName + " " + d.Child.LastName).Contains(term) ||
                d.Child.Group.Name.Contains(term) ||
                matchingTypes.Contains(d.Type))
            .OrderByDescending(d => d.UploadedOn)
            .Select(d => d.Title)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }

    public async Task<ChildDocumentDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var canAccess = await CanAccessAsync(id, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return null;
        }

        return await context.ChildDocuments
            .Where(d => !d.IsDeleted && d.Id == id)
            .Select(d => new ChildDocumentDetailsViewModel
            {
                Id = d.Id,
                ChildId = d.ChildId,
                ChildFullName = d.Child.FirstName + " " + d.Child.LastName,
                GroupName = d.Child.Group.Name,
                Type = d.Type,
                Title = d.Title,
                FileUrl = d.FileUrl,
                Status = d.Status,
                UploadedByEmail = context.Users
                    .Where(u => u.Id == d.UploadedByUserId)
                    .Select(u => u.Email!)
                    .FirstOrDefault()!,
                UploadedOn = d.UploadedOn,
                ReviewNote = d.ReviewNote,
                ReviewedOn = d.ReviewedOn,
                CanReview = isAdmin && d.Status == RequestStatus.Pending
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ChildDocumentCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new ChildDocumentCreateViewModel
        {
            Children = await GetChildrenSelectListAsync(userId, isAdmin, isTeacher)
        };
    }

    public async Task CreateAsync(ChildDocumentCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!model.ChildId.HasValue)
        {
            throw new InvalidOperationException("Child is required.");
        }

        var canUploadForChild = await CanUploadForChildAsync(model.ChildId.Value, userId, isAdmin, isTeacher);

        if (!canUploadForChild)
        {
            throw new InvalidOperationException("Child not found.");
        }

        var childDocument = new ChildDocument
        {
            ChildId = model.ChildId.Value,
            Type = model.Type,
            Title = model.Title,
            FileUrl = model.FileUrl ?? throw new InvalidOperationException("Document file is required."),
            UploadedByUserId = userId
        };

        if (isAdmin)
        {
            childDocument.Status = RequestStatus.Approved;
            childDocument.ReviewedByUserId = userId;
            childDocument.ReviewedOn = DateTime.UtcNow;
            childDocument.ReviewNote = "Approved on upload.";
        }

        await context.ChildDocuments.AddAsync(childDocument);
        await context.SaveChangesAsync();
    }

    public async Task<ChildDocumentReviewViewModel?> GetForReviewAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            return null;
        }

        return await context.ChildDocuments
            .Where(d => !d.IsDeleted && d.Id == id && d.Status == RequestStatus.Pending)
            .Select(d => new ChildDocumentReviewViewModel
            {
                Id = d.Id,
                ChildFullName = d.Child.FirstName + " " + d.Child.LastName,
                Title = d.Title,
                Type = d.Type,
                FileUrl = d.FileUrl,
                Status = d.Status,
                ReviewNote = d.ReviewNote
            })
            .FirstOrDefaultAsync();
    }

    public async Task ReviewAsync(ChildDocumentReviewViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            throw new InvalidOperationException("Child document not found.");
        }

        var childDocument = await context.ChildDocuments
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == model.Id);

        if (childDocument == null)
        {
            throw new InvalidOperationException("Child document not found.");
        }

        if (childDocument.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending documents can be reviewed.");
        }

        childDocument.Status = model.Status;
        childDocument.ReviewNote = model.ReviewNote;
        childDocument.ReviewedByUserId = userId;
        childDocument.ReviewedOn = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    private async Task<bool> CanAccessAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.ChildDocuments
                .AnyAsync(d => !d.IsDeleted && d.Id == id);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await context.ChildDocuments.AnyAsync(d =>
                       !d.IsDeleted &&
                       d.Id == id &&
                       d.Child.GroupId == teacherGroupId.Value);
        }

        return await context.ChildDocuments.AnyAsync(d =>
            !d.IsDeleted &&
            d.Id == id &&
            d.Child.Parent != null &&
            !d.Child.Parent.IsDeleted &&
            d.Child.Parent.UserId == userId);
    }

    private async Task<bool> CanUploadForChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Children
                .AnyAsync(c => !c.IsDeleted && c.Id == childId);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await context.Children.AnyAsync(c =>
                       !c.IsDeleted &&
                       c.Id == childId &&
                       c.GroupId == teacherGroupId.Value);
        }

        return await context.Children.AnyAsync(c =>
            !c.IsDeleted &&
            c.Id == childId &&
            c.Parent != null &&
            !c.Parent.IsDeleted &&
            c.Parent.UserId == userId);
    }

    private async Task<IEnumerable<SelectListItem>> GetChildrenSelectListAsync(string userId, bool isAdmin, bool isTeacher)
    {
        var query = context.Children
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<SelectListItem>();
            }

            query = query.Where(c => c.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(c =>
                c.Parent != null &&
                !c.Parent.IsDeleted &&
                c.Parent.UserId == userId);
        }

        return await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FirstName + " " + c.LastName
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
