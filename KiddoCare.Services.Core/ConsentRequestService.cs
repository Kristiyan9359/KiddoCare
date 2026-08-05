using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ConsentRequests;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class ConsentRequestService : IConsentRequestService
{
    private readonly ApplicationDbContext context;

    public ConsentRequestService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<ConsentRequestIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? statusFilter)
    {
        var query = context.ConsentRequests
            .Where(c => !c.IsDeleted && !c.Child.IsDeleted)
            .AsQueryable();

        int? teacherGroupId = null;

        if (isTeacher && !isAdmin)
        {
            teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<ConsentRequestIndexViewModel>();
            }

            query = query.Where(c => c.Child.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(c =>
                c.Child.Parent != null &&
                !c.Child.Parent.IsDeleted &&
                c.Child.Parent.UserId == userId);
        }

        if (statusFilter == "pending")
        {
            query = query.Where(c => c.Status == RequestStatus.Pending);
        }
        else if (statusFilter == "approved")
        {
            query = query.Where(c => c.Status == RequestStatus.Approved);
        }
        else if (statusFilter == "rejected")
        {
            query = query.Where(c => c.Status == RequestStatus.Rejected);
        }

        return await query
            .OrderByDescending(c => c.CreatedOn)
            .Select(c => new ConsentRequestIndexViewModel
            {
                Id = c.Id,
                ChildFullName = c.Child.FirstName + " " + c.Child.LastName,
                GroupName = c.Child.Group.Name,
                Title = c.Title,
                Type = c.Type,
                Status = c.Status,
                CreatedOn = c.CreatedOn,
                CanRespond =
                    c.Status == RequestStatus.Pending &&
                    c.Child.Parent != null &&
                    !c.Child.Parent.IsDeleted &&
                    c.Child.Parent.UserId == userId
            })
            .ToListAsync();
    }

    public async Task<ConsentRequestDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var canAccess = await CanAccessAsync(id, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return null;
        }

        return await context.ConsentRequests
            .Where(c => !c.IsDeleted && c.Id == id)
            .Select(c => new ConsentRequestDetailsViewModel
            {
                Id = c.Id,
                ChildId = c.ChildId,
                ChildFullName = c.Child.FirstName + " " + c.Child.LastName,
                GroupName = c.Child.Group.Name,
                Title = c.Title,
                Description = c.Description,
                Type = c.Type,
                Status = c.Status,
                CreatedOn = c.CreatedOn,
                CreatedByEmail = context.Users
                    .Where(u => u.Id == c.CreatedByUserId)
                    .Select(u => u.Email!)
                    .FirstOrDefault()!,
                ParentNote = c.ParentNote,
                RespondedOn = c.RespondedOn,
                CanRespond =
                    c.Status == RequestStatus.Pending &&
                    c.Child.Parent != null &&
                    !c.Child.Parent.IsDeleted &&
                    c.Child.Parent.UserId == userId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ConsentRequestCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new ConsentRequestCreateViewModel
        {
            Children = await GetChildrenSelectListAsync(userId, isAdmin, isTeacher)
        };
    }

    public async Task CreateAsync(ConsentRequestCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin && !isTeacher)
        {
            throw new InvalidOperationException("Consent request not found.");
        }

        if (!model.ChildId.HasValue)
        {
            throw new InvalidOperationException("Child is required.");
        }

        var canCreateForChild = await CanCreateForChildAsync(model.ChildId.Value, userId, isAdmin, isTeacher);

        if (!canCreateForChild)
        {
            throw new InvalidOperationException("Child not found.");
        }

        if (model.Type == ConsentRequestType.Other && string.IsNullOrWhiteSpace(model.Description))
        {
            throw new InvalidOperationException("Description is required when type is Other.");
        }

        var hasDuplicatePendingRequest = await context.ConsentRequests.AnyAsync(c =>
            !c.IsDeleted &&
            c.ChildId == model.ChildId.Value &&
            c.Type == model.Type &&
            c.Status == RequestStatus.Pending);

        if (hasDuplicatePendingRequest)
        {
            throw new InvalidOperationException("There is already a pending consent request of this type for this child.");
        }

        var consentRequest = new ConsentRequest
        {
            ChildId = model.ChildId.Value,
            Title = model.Title,
            Description = model.Description,
            Type = model.Type,
            CreatedByUserId = userId
        };

        await context.ConsentRequests.AddAsync(consentRequest);
        await context.SaveChangesAsync();
    }

    public async Task<ConsentRequestRespondViewModel?> GetForRespondAsync(int id, string userId)
    {
        var canRespond = await CanRespondAsync(id, userId);

        if (!canRespond)
        {
            return null;
        }

        return await context.ConsentRequests
            .Where(c => !c.IsDeleted && c.Id == id)
            .Select(c => new ConsentRequestRespondViewModel
            {
                Id = c.Id,
                ChildFullName = c.Child.FirstName + " " + c.Child.LastName,
                Title = c.Title,
                Type = c.Type,
                Description = c.Description,
                Status = c.Status,
                ParentNote = c.ParentNote
            })
            .FirstOrDefaultAsync();
    }

    public async Task RespondAsync(ConsentRequestRespondViewModel model, string userId)
    {
        var canRespond = await CanRespondAsync(model.Id, userId);

        if (!canRespond)
        {
            throw new InvalidOperationException("Consent request not found.");
        }

        var consentRequest = await context.ConsentRequests
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.Id == model.Id);

        if (consentRequest == null)
        {
            throw new InvalidOperationException("Consent request not found.");
        }

        if (consentRequest.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending consent requests can be responded to.");
        }

        consentRequest.Status = model.Status;
        consentRequest.ParentNote = model.ParentNote;
        consentRequest.RespondedByUserId = userId;
        consentRequest.RespondedOn = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    private async Task<bool> CanAccessAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.ConsentRequests
                .AnyAsync(c => !c.IsDeleted && c.Id == id);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await context.ConsentRequests.AnyAsync(c =>
                       !c.IsDeleted &&
                       c.Id == id &&
                       c.Child.GroupId == teacherGroupId.Value);
        }

        return await context.ConsentRequests.AnyAsync(c =>
            !c.IsDeleted &&
            c.Id == id &&
            c.Child.Parent != null &&
            !c.Child.Parent.IsDeleted &&
            c.Child.Parent.UserId == userId);
    }

    private async Task<bool> CanCreateForChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Children
                .AnyAsync(c => !c.IsDeleted && c.Id == childId);
        }

        if (!isTeacher)
        {
            return false;
        }

        var teacherGroupId = await GetTeacherGroupIdAsync(userId);

        return teacherGroupId.HasValue &&
               await context.Children.AnyAsync(c =>
                   !c.IsDeleted &&
                   c.Id == childId &&
                   c.GroupId == teacherGroupId.Value);
    }

    private async Task<bool> CanRespondAsync(int id, string userId)
    {
        return await context.ConsentRequests.AnyAsync(c =>
            !c.IsDeleted &&
            c.Id == id &&
            c.Status == RequestStatus.Pending &&
            c.Child.Parent != null &&
            !c.Child.Parent.IsDeleted &&
            c.Child.Parent.UserId == userId);
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
            return new List<SelectListItem>();
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