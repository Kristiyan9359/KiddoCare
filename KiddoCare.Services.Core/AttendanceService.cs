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

    public async Task<AttendanceDailyViewModel> GetDailyAttendanceAsync(DateTime date, int? groupId)
    {
        var normalizedDate = date.Date;

        var groups = await context.KindergartenGroups
            .Where(g => !g.IsDeleted)
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

        if (groupId.HasValue)
        {
            childrenQuery = childrenQuery.Where(c => c.GroupId == groupId.Value);
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
            GroupId = groupId,
            Groups = groups,
            Children = childViewModels,
            Summary = summary
        };
    }

    public async Task SaveDailyAttendanceAsync(AttendanceDailyViewModel model)
    {
        var normalizedDate = model.Date.Date;

        foreach (var childModel in model.Children)
        {
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
}