using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.AbsenceRequests;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class AbsenceRequestService : IAbsenceRequestService
{
    private readonly ApplicationDbContext context;

    public AbsenceRequestService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<AbsenceRequestListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, string? statusFilter, int page, int pageSize)
    {
        var query = context.AbsenceRequests
            .Where(a =>
                !a.IsDeleted &&
                !a.Child.IsDeleted &&
                a.Status != RequestStatus.Rejected)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new AbsenceRequestListViewModel
                {
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    Page = 1,
                    PageSize = pageSize
                };
            }

            query = query.Where(a => a.Child.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(a =>
                a.Child.Parent != null &&
                !a.Child.Parent.IsDeleted &&
                a.Child.Parent.UserId == userId);
        }

        if (statusFilter == "pending")
        {
            query = query.Where(a => a.Status == RequestStatus.Pending);
        }
        else if (statusFilter == "confirmed" || statusFilter == "approved")
        {
            query = query.Where(a => a.Status == RequestStatus.Approved);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            var matchingReasons = Enum.GetValues<AbsenceReason>()
                .Where(r => r.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            query = query.Where(a =>
                (a.Child.FirstName + " " + a.Child.LastName).Contains(searchTerm) ||
                a.Child.Group.Name.Contains(searchTerm) ||
                matchingReasons.Contains(a.Reason));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalAbsenceRequests = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalAbsenceRequests / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var absenceRequests = await query
            .OrderByDescending(a => a.RequestedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AbsenceRequestIndexViewModel
            {
                Id = a.Id,
                ChildFullName = a.Child.FirstName + " " + a.Child.LastName,
                GroupName = a.Child.Group.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Reason = a.Reason,
                Status = a.Status,
                CanReview = (isAdmin || isTeacher) && a.Status == RequestStatus.Pending
            })
            .ToListAsync();

        return new AbsenceRequestListViewModel
        {
            AbsenceRequests = absenceRequests,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            Page = page,
            PageSize = pageSize,
            TotalAbsenceRequests = totalAbsenceRequests
        };
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        var query = context.AbsenceRequests
            .Where(a =>
                !a.IsDeleted &&
                !a.Child.IsDeleted &&
                a.Status != RequestStatus.Rejected)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<string>();
            }

            query = query.Where(a => a.Child.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(a =>
                a.Child.Parent != null &&
                !a.Child.Parent.IsDeleted &&
                a.Child.Parent.UserId == userId);
        }

        var matchingReasons = Enum.GetValues<AbsenceReason>()
            .Where(r => r.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Where(a =>
                (a.Child.FirstName + " " + a.Child.LastName).Contains(term) ||
                a.Child.Group.Name.Contains(term) ||
                matchingReasons.Contains(a.Reason))
            .OrderByDescending(a => a.RequestedOn)
            .Select(a => a.Child.FirstName + " " + a.Child.LastName)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }

    public async Task<AbsenceRequestDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var canAccess = await CanAccessAsync(id, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return null;
        }

        return await context.AbsenceRequests
            .Where(a => !a.IsDeleted && a.Id == id)
            .Where(a => a.Status != RequestStatus.Rejected)
            .Select(a => new AbsenceRequestDetailsViewModel
            {
                Id = a.Id,
                ChildId = a.ChildId,
                ChildFullName = a.Child.FirstName + " " + a.Child.LastName,
                GroupName = a.Child.Group.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Reason = a.Reason,
                ParentNote = a.ParentNote,
                Status = a.Status,
                RequestedByEmail = context.Users
                    .Where(u => u.Id == a.RequestedByUserId)
                    .Select(u => u.Email!)
                    .FirstOrDefault()!,
                RequestedOn = a.RequestedOn,
                ReviewNote = a.ReviewNote,
                ReviewedOn = a.ReviewedOn,
                CanReview = (isAdmin || isTeacher) && a.Status == RequestStatus.Pending
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AbsenceRequestCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new AbsenceRequestCreateViewModel
        {
            Children = await GetChildrenSelectListAsync(userId, isAdmin, isTeacher)
        };
    }

    public async Task CreateAsync(AbsenceRequestCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!model.ChildId.HasValue)
        {
            throw new InvalidOperationException("Child is required.");
        }

        if (model.StartDate.Date < DateTime.Today)
        {
            throw new InvalidOperationException("Start date cannot be in the past.");
        }

        if (model.EndDate.Date < model.StartDate.Date)
        {
            throw new InvalidOperationException("End date cannot be before start date.");
        }

        var canCreateForChild = await CanCreateForChildAsync(model.ChildId.Value, userId, isAdmin, isTeacher);

        if (!canCreateForChild)
        {
            throw new InvalidOperationException("Child not found.");
        }

        var hasOverlappingRequest = await context.AbsenceRequests.AnyAsync(a =>
            !a.IsDeleted &&
            a.ChildId == model.ChildId.Value &&
            a.Status != RequestStatus.Rejected &&
            a.StartDate <= model.EndDate.Date &&
            a.EndDate >= model.StartDate.Date);

        if (hasOverlappingRequest)
        {
            throw new InvalidOperationException("There is already an active absence notice for this child in the selected period.");
        }

        var absenceRequest = new AbsenceRequest
        {
            ChildId = model.ChildId.Value,
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            Reason = model.Reason,
            ParentNote = model.ParentNote,
            RequestedByUserId = userId
        };

        await context.AbsenceRequests.AddAsync(absenceRequest);

        if (isAdmin || isTeacher)
        {
            absenceRequest.Status = RequestStatus.Approved;
            absenceRequest.ReviewedByUserId = userId;
            absenceRequest.ReviewedOn = DateTime.UtcNow;
            absenceRequest.ReviewNote = "Confirmed on creation.";

            await ApplyConfirmedAbsenceToAttendanceAsync(absenceRequest);
        }

        await context.SaveChangesAsync();
    }

    public async Task<AbsenceRequestReviewViewModel?> GetForReviewAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        var canReview = await CanReviewAsync(id, userId, isAdmin, isTeacher);

        if (!canReview)
        {
            return null;
        }

        return await context.AbsenceRequests
            .Where(a => !a.IsDeleted && a.Id == id)
            .Select(a => new AbsenceRequestReviewViewModel
            {
                Id = a.Id,
                ChildFullName = a.Child.FirstName + " " + a.Child.LastName,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Reason = a.Reason,
                ParentNote = a.ParentNote,
                ReviewNote = a.ReviewNote
            })
            .FirstOrDefaultAsync();
    }

    public async Task ReviewAsync(AbsenceRequestReviewViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        var canReview = await CanReviewAsync(model.Id, userId, isAdmin, isTeacher);

        if (!canReview)
        {
            throw new InvalidOperationException("Absence notice not found.");
        }

        var absenceRequest = await context.AbsenceRequests
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.Id == model.Id);

        if (absenceRequest == null)
        {
            throw new InvalidOperationException("Absence notice not found.");
        }

        if (absenceRequest.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending notices can be confirmed.");
        }

        absenceRequest.Status = RequestStatus.Approved;
        absenceRequest.ReviewNote = model.ReviewNote;
        absenceRequest.ReviewedByUserId = userId;
        absenceRequest.ReviewedOn = DateTime.UtcNow;

        await ApplyConfirmedAbsenceToAttendanceAsync(absenceRequest);

        await context.SaveChangesAsync();
    }

    private async Task<bool> CanAccessAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.AbsenceRequests
                .AnyAsync(a =>
                    !a.IsDeleted &&
                    a.Id == id &&
                    a.Status != RequestStatus.Rejected);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await context.AbsenceRequests.AnyAsync(a =>
                       !a.IsDeleted &&
                       a.Id == id &&
                       a.Status != RequestStatus.Rejected &&
                       a.Child.GroupId == teacherGroupId.Value);
        }

        return await context.AbsenceRequests.AnyAsync(a =>
            !a.IsDeleted &&
            a.Id == id &&
            a.Status != RequestStatus.Rejected &&
            a.Child.Parent != null &&
            !a.Child.Parent.IsDeleted &&
            a.Child.Parent.UserId == userId);
    }

    private async Task<bool> CanReviewAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.AbsenceRequests
                .AnyAsync(a =>
                    !a.IsDeleted &&
                    a.Id == id &&
                    a.Status == RequestStatus.Pending);
        }

        if (!isTeacher)
        {
            return false;
        }

        var teacherGroupId = await GetTeacherGroupIdAsync(userId);

        return teacherGroupId.HasValue &&
               await context.AbsenceRequests.AnyAsync(a =>
                   !a.IsDeleted &&
                   a.Id == id &&
                   a.Status == RequestStatus.Pending &&
                   a.Child.GroupId == teacherGroupId.Value);
    }

    private async Task<bool> CanCreateForChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
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

    private async Task ApplyConfirmedAbsenceToAttendanceAsync(AbsenceRequest absenceRequest)
    {
        var status = absenceRequest.Reason switch
        {
            AbsenceReason.Sick => AttendanceStatus.Sick,
            AbsenceReason.Vacation => AttendanceStatus.Vacation,
            _ => AttendanceStatus.Absent
        };

        var currentDate = absenceRequest.StartDate.Date;
        var endDate = absenceRequest.EndDate.Date;

        while (currentDate <= endDate)
        {
            var attendanceRecord = await context.AttendanceRecords
                .FirstOrDefaultAsync(a =>
                    a.ChildId == absenceRequest.ChildId &&
                    a.Date == currentDate);

            if (attendanceRecord == null)
            {
                attendanceRecord = new AttendanceRecord
                {
                    ChildId = absenceRequest.ChildId,
                    Date = currentDate,
                    Status = status,
                    Note = "Created from confirmed absence notice."
                };

                await context.AttendanceRecords.AddAsync(attendanceRecord);
            }
            else
            {
                attendanceRecord.Status = status;
                attendanceRecord.Note = "Updated from confirmed absence notice.";
            }

            currentDate = currentDate.AddDays(1);
        }
    }
    private async Task<int?> GetTeacherGroupIdAsync(string userId)
    {
        return await context.TeacherProfiles
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .Select(t => (int?)t.GroupId)
            .FirstOrDefaultAsync();
    }
}
