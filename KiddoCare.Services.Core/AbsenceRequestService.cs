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

    public async Task<IEnumerable<AbsenceRequestIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? statusFilter)
    {
        var query = context.AbsenceRequests
            .Where(a => !a.IsDeleted && !a.Child.IsDeleted)
            .AsQueryable();

        int? teacherGroupId = null;

        if (isTeacher && !isAdmin)
        {
            teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<AbsenceRequestIndexViewModel>();
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
            query = query.Where(a => a.Status == AbsenceRequestStatus.Pending);
        }
        else if (statusFilter == "approved")
        {
            query = query.Where(a => a.Status == AbsenceRequestStatus.Approved);
        }
        else if (statusFilter == "rejected")
        {
            query = query.Where(a => a.Status == AbsenceRequestStatus.Rejected);
        }

        return await query
            .OrderByDescending(a => a.RequestedOn)
            .Select(a => new AbsenceRequestIndexViewModel
            {
                Id = a.Id,
                ChildFullName = a.Child.FirstName + " " + a.Child.LastName,
                GroupName = a.Child.Group.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Reason = a.Reason,
                Status = a.Status,
                CanReview = (isAdmin || isTeacher) && a.Status == AbsenceRequestStatus.Pending
            })
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
                CanReview = (isAdmin || isTeacher) && a.Status == AbsenceRequestStatus.Pending
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
            a.Status != AbsenceRequestStatus.Rejected &&
            a.StartDate <= model.EndDate.Date &&
            a.EndDate >= model.StartDate.Date);

        if (hasOverlappingRequest)
        {
            throw new InvalidOperationException("There is already an active absence request for this child in the selected period.");
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
                Status = a.Status,
                ReviewNote = a.ReviewNote
            })
            .FirstOrDefaultAsync();
    }

    public async Task ReviewAsync(AbsenceRequestReviewViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        var canReview = await CanReviewAsync(model.Id, userId, isAdmin, isTeacher);

        if (!canReview)
        {
            throw new InvalidOperationException("Absence request not found.");
        }

        var absenceRequest = await context.AbsenceRequests
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.Id == model.Id);

        if (absenceRequest == null)
        {
            throw new InvalidOperationException("Absence request not found.");
        }

        if (absenceRequest.Status != AbsenceRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be reviewed.");
        }

        absenceRequest.Status = model.Status;
        absenceRequest.ReviewNote = model.ReviewNote;
        absenceRequest.ReviewedByUserId = userId;
        absenceRequest.ReviewedOn = DateTime.UtcNow;

        if (absenceRequest.Status == AbsenceRequestStatus.Approved)
        {
            await ApplyApprovedAbsenceToAttendanceAsync(absenceRequest);
        }

        await context.SaveChangesAsync();
    }

    private async Task<bool> CanAccessAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.AbsenceRequests
                .AnyAsync(a => !a.IsDeleted && a.Id == id);
        }

        if (isTeacher)
        {
            var teacherGroupId = await GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await context.AbsenceRequests.AnyAsync(a =>
                       !a.IsDeleted &&
                       a.Id == id &&
                       a.Child.GroupId == teacherGroupId.Value);
        }

        return await context.AbsenceRequests.AnyAsync(a =>
            !a.IsDeleted &&
            a.Id == id &&
            a.Child.Parent != null &&
            !a.Child.Parent.IsDeleted &&
            a.Child.Parent.UserId == userId);
    }

    private async Task<bool> CanReviewAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.AbsenceRequests
                .AnyAsync(a => !a.IsDeleted && a.Id == id);
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
                   a.Child.GroupId == teacherGroupId.Value);
    }

    private async Task<bool> CanCreateForChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin || isTeacher)
        {
            return await context.Children
                .AnyAsync(c => !c.IsDeleted && c.Id == childId);
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

    private async Task ApplyApprovedAbsenceToAttendanceAsync(AbsenceRequest absenceRequest)
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
                    Note = "Created from approved absence request."
                };

                await context.AttendanceRecords.AddAsync(attendanceRecord);
            }
            else
            {
                attendanceRecord.Status = status;
                attendanceRecord.Note = "Updated from approved absence request.";
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