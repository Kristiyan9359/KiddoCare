using KiddoCare.Data;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.ActivityFeed;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class ActivityFeedService : IActivityFeedService
{
    private readonly ApplicationDbContext context;

    public ActivityFeedService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<ChildActivityFeedViewModel?> GetChildFeedAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        var child = await context.Children
            .Where(c => !c.IsDeleted && c.Id == childId)
            .Select(c => new
            {
                c.Id,
                FullName = c.FirstName + " " + c.LastName,
                c.GroupId,
                ParentUserId = c.Parent == null ? null : c.Parent.UserId
            })
            .FirstOrDefaultAsync();

        if (child == null)
        {
            return null;
        }

        var canAccess = await CanAccessChildAsync(childId, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return null;
        }

        var items = new List<ActivityFeedItemViewModel>();

        var attendanceItems = await context.AttendanceRecords
            .Where(a => a.ChildId == childId)
            .OrderByDescending(a => a.Date)
            .Take(10)
            .Select(a => new ActivityFeedItemViewModel
            {
                Date = a.Date,
                Type = "Attendance",
                Title = "Attendance marked",
                Description = a.Status.ToString(),
                ActionController = "Attendance",
                ActionName = "History"
            })
            .ToListAsync();

        items.AddRange(attendanceItems);

        var dailyReportItems = await context.DailyReports
            .Where(r => !r.IsDeleted && r.ChildId == childId)
            .OrderByDescending(r => r.ReportDate)
            .Take(10)
            .Select(r => new ActivityFeedItemViewModel
            {
                Date = r.ReportDate,
                Type = "Daily Report",
                Title = "Daily report added",
                Description = "Mood: " + r.Mood,
                ActionController = "DailyReports",
                ActionName = "Details",
                RouteId = r.Id
            })
            .ToListAsync();

        items.AddRange(dailyReportItems);

        var absenceRequestItems = await context.AbsenceRequests
          .Where(a => !a.IsDeleted && a.ChildId == childId)
          .OrderByDescending(a => a.RequestedOn)
          .Take(10)
          .Select(a => new ActivityFeedItemViewModel
          {
              Date = a.RequestedOn,
              Type = "Absence Request",
              Title = "Absence request submitted",
              Description = a.Reason + " - " + a.Status,
              ActionController = "AbsenceRequests",
              ActionName = "Details",
              RouteId = a.Id
          })
          .ToListAsync();

        items.AddRange(absenceRequestItems);

        var consentRequestItems = await context.ConsentRequests
          .Where(c => !c.IsDeleted && c.ChildId == childId)
          .OrderByDescending(c => c.CreatedOn)
          .Take(10)
          .Select(c => new ActivityFeedItemViewModel
          {
              Date = c.CreatedOn,
              Type = "Consent Request",
              Title = c.Title,
              Description = c.Type + " - " + c.Status,
              ActionController = "ConsentRequests",
              ActionName = "Details",
              RouteId = c.Id
          })
          .ToListAsync();

        items.AddRange(consentRequestItems);

        var eventItems = await context.Events
            .Where(e =>
                !e.IsDeleted &&
                e.IsPublic &&
                e.StartDateTime >= DateTime.Today &&
                (e.GroupId == null || e.GroupId == child.GroupId))
            .OrderBy(e => e.StartDateTime)
            .Take(10)
            .Select(e => new ActivityFeedItemViewModel
            {
                Date = e.StartDateTime,
                Type = "Event",
                Title = e.Title,
                Description = e.Location,
                ActionController = "Events",
                ActionName = "Details",
                RouteId = e.Id
            })
            .ToListAsync();

        items.AddRange(eventItems);

        var announcementItems = await context.Announcements
            .Where(a =>
                !a.IsDeleted &&
                (a.GroupId == null || a.GroupId == child.GroupId))
            .OrderByDescending(a => a.PublishedOn)
            .Take(10)
            .Select(a => new ActivityFeedItemViewModel
            {
                Date = a.PublishedOn,
                Type = "Announcement",
                Title = a.Title,
                Description = a.Group == null ? "All groups" : a.Group.Name,
                ActionController = "Announcements",
                ActionName = "Details",
                RouteId = a.Id
            })
            .ToListAsync();

        items.AddRange(announcementItems);

        return new ChildActivityFeedViewModel
        {
            ChildId = child.Id,
            ChildFullName = child.FullName,
            Items = items
                .OrderByDescending(i => i.Date)
                .Take(20)
                .ToList()
        };
    }

    private async Task<bool> CanAccessChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Children
                .AnyAsync(c => !c.IsDeleted && c.Id == childId);
        }

        if (isTeacher)
        {
            var teacherGroupId = await context.TeacherProfiles
                .Where(t => !t.IsDeleted && t.UserId == userId)
                .Select(t => (int?)t.GroupId)
                .FirstOrDefaultAsync();

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
}