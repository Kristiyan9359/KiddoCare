using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Attendance;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext context;

    public AttendanceService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<AttendanceDailyViewModel> GetDailyAttendanceAsync(
        DateTime date,
        int? groupId,
        string userId,
        bool isAdmin,
        bool isTeacher)
    {
        var normalizedDate = date.Date;
        var effectiveGroupId = groupId;

        if (isTeacher && !isAdmin)
        {
            effectiveGroupId = await GetTeacherGroupIdAsync(userId);

            if (effectiveGroupId == null)
            {
                return new AttendanceDailyViewModel
                {
                    Date = normalizedDate
                };
            }
        }

        var groupsQuery = context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin && effectiveGroupId.HasValue)
        {
            groupsQuery = groupsQuery.Where(g => g.Id == effectiveGroupId.Value);
        }

        var groups = await groupsQuery
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();

        var childrenQuery = context.Children
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (effectiveGroupId.HasValue)
        {
            childrenQuery = childrenQuery.Where(c => c.GroupId == effectiveGroupId.Value);
        }

        var existingRecords = await context.AttendanceRecords
            .Where(a => a.Date == normalizedDate)
            .ToListAsync();

        var children = await childrenQuery
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new
            {
                c.Id,
                FullName = c.FirstName + " " + c.LastName
            })
            .ToListAsync();

        var childViewModels = children
            .Select(c =>
            {
                var existingRecord = existingRecords.FirstOrDefault(a => a.ChildId == c.Id);

                return new AttendanceChildViewModel
                {
                    ChildId = c.Id,
                    FullName = c.FullName,
                    Status = existingRecord?.Status ?? AttendanceStatus.Present,
                    Note = existingRecord?.Note
                };
            })
            .ToList();

        var summary = new AttendanceSummaryViewModel
        {
            PresentCount = childViewModels.Count(c => c.Status == AttendanceStatus.Present),
            AbsentCount = childViewModels.Count(c => c.Status == AttendanceStatus.Absent),
            SickCount = childViewModels.Count(c => c.Status == AttendanceStatus.Sick),
            VacationCount = childViewModels.Count(c => c.Status == AttendanceStatus.Vacation),
            LateCount = childViewModels.Count(c => c.Status == AttendanceStatus.Late),
            TotalCount = childViewModels.Count
        };

        return new AttendanceDailyViewModel
        {
            Date = normalizedDate,
            GroupId = effectiveGroupId,
            Groups = groups,
            Children = childViewModels,
            Summary = summary
        };
    }

    public async Task SaveDailyAttendanceAsync(
        AttendanceDailyViewModel model,
        string userId,
        bool isAdmin,
        bool isTeacher)
    {
        var normalizedDate = model.Date.Date;

        int? teacherGroupId = null;

        if (isTeacher && !isAdmin)
        {
            teacherGroupId = await GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                throw new InvalidOperationException("Teacher group not found.");
            }
        }

        foreach (var childModel in model.Children)
        {
            var child = await context.Children
                .FirstOrDefaultAsync(c => c.Id == childModel.ChildId && !c.IsDeleted);

            if (child == null)
            {
                continue;
            }

            if (teacherGroupId.HasValue && child.GroupId != teacherGroupId.Value)
            {
                continue;
            }

            var existingRecord = await context.AttendanceRecords
                .FirstOrDefaultAsync(a =>
                    a.ChildId == childModel.ChildId &&
                    a.Date == normalizedDate);

            if (existingRecord == null)
            {
                var record = new AttendanceRecord
                {
                    ChildId = childModel.ChildId,
                    Date = normalizedDate,
                    Status = childModel.Status,
                    Note = childModel.Note
                };

                await context.AttendanceRecords.AddAsync(record);
            }
            else
            {
                existingRecord.Status = childModel.Status;
                existingRecord.Note = childModel.Note;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<AttendanceFilterViewModel> GetHistoryAsync(
        AttendanceFilterViewModel filter,
        string userId,
        bool isAdmin,
        bool isTeacher)
    {
        var effectiveGroupId = filter.GroupId;

        if (isTeacher && !isAdmin)
        {
            effectiveGroupId = await GetTeacherGroupIdAsync(userId);

            if (effectiveGroupId == null)
            {
                filter.Groups = new List<SelectListItem>();
                filter.Records = new List<AttendanceRecordViewModel>();

                return filter;
            }
        }

        var groupsQuery = context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin && effectiveGroupId.HasValue)
        {
            groupsQuery = groupsQuery.Where(g => g.Id == effectiveGroupId.Value);
        }

        var groups = await groupsQuery
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();

        var query = context.AttendanceRecords
            .Include(a => a.Child)
            .ThenInclude(c => c.Group)
            .AsQueryable();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.Date <= filter.ToDate.Value.Date);
        }

        if (effectiveGroupId.HasValue)
        {
            query = query.Where(a => a.Child.GroupId == effectiveGroupId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        var records = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Child.FirstName)
            .ThenBy(a => a.Child.LastName)
            .Select(a => new AttendanceRecordViewModel
            {
                Date = a.Date,
                ChildName = a.Child.FirstName + " " + a.Child.LastName,
                GroupName = a.Child.Group.Name,
                Status = a.Status,
                Note = a.Note
            })
            .ToListAsync();

        filter.GroupId = effectiveGroupId;
        filter.Groups = groups;
        filter.Records = records;

        return filter;
    }

    private async Task<int?> GetTeacherGroupIdAsync(string userId)
    {
        return await context.TeacherProfiles
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .Select(t => (int?)t.GroupId)
            .FirstOrDefaultAsync();
    }
}