namespace KiddoCare.Services.Core;

using KiddoCare.Data;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Dashboard;
using KiddoCare.ViewModels.Events;
using Microsoft.EntityFrameworkCore;


public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext context;

    public DashboardService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var today = DateTime.Today;

        var groupsCount = await context.KindergartenGroups
            .CountAsync(g => !g.IsDeleted);

        var childrenCount = await context.Children
            .CountAsync(c => !c.IsDeleted);

        var todayAttendance = await context.AttendanceRecords
            .Where(a => a.Date == today)
            .ToListAsync();

        var upcomingEvents = await context.Events
            .Where(e => !e.IsDeleted && e.StartDateTime >= DateTime.Now)
            .OrderBy(e => e.StartDateTime)
            .Take(5)
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

        var recentAnnouncements = await context.Announcements
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.PublishedOn)
            .Take(5)
            .Select(a => new DashboardAnnouncementViewModel
            {
                Id = a.Id,
                Title = a.Title,
                PublishedOn = a.PublishedOn,
                GroupName = a.Group == null ? "All groups" : a.Group.Name
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            GroupsCount = groupsCount,
            ChildrenCount = childrenCount,
            PresentTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Present),
            AbsentTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent),
            SickTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Sick),
            LateTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Late),
            VacationTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Vacation),
            UpcomingEvents = upcomingEvents,
            RecentAnnouncements = recentAnnouncements
        };
    }

    public async Task<ParentDashboardViewModel> GetParentDashboardAsync(string userId)
    {
        var children = await context.Children
            .Where(c =>
                !c.IsDeleted &&
                c.Parent != null &&
                c.Parent.UserId == userId)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new ParentDashboardChildViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                PhotoUrl = c.PhotoUrl,
                GroupName = c.Group.Name,
                LastDailyReportDate = c.DailyReports
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.ReportDate)
                    .Select(r => (DateTime?)r.ReportDate)
                    .FirstOrDefault(),
                LastDailyReportMood = c.DailyReports
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.ReportDate)
                    .Select(r => (ChildMood?)r.Mood)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var groupIds = await context.Children
            .Where(c =>
                !c.IsDeleted &&
                c.Parent != null &&
                c.Parent.UserId == userId)
            .Select(c => c.GroupId)
            .Distinct()
            .ToListAsync();

        var upcomingEvents = await context.Events
            .Where(e =>
                !e.IsDeleted &&
                e.IsPublic &&
                e.StartDateTime >= DateTime.Now &&
                (e.GroupId == null || groupIds.Contains(e.GroupId.Value)))
            .OrderBy(e => e.StartDateTime)
            .Take(5)
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

        var recentAnnouncements = await context.Announcements
            .Where(a =>
                !a.IsDeleted &&
                (a.GroupId == null || groupIds.Contains(a.GroupId.Value)))
            .OrderByDescending(a => a.PublishedOn)
            .Take(5)
            .Select(a => new DashboardAnnouncementViewModel
            {
                Id = a.Id,
                Title = a.Title,
                PublishedOn = a.PublishedOn,
                GroupName = a.Group == null ? "All groups" : a.Group.Name
            })
            .ToListAsync();

        return new ParentDashboardViewModel
        {
            Children = children,
            UpcomingEvents = upcomingEvents,
            RecentAnnouncements = recentAnnouncements
        };
    }

    public async Task<TeacherDashboardViewModel?> GetTeacherDashboardAsync(string userId)
    {
        var teacher = await context.TeacherProfiles
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .Select(t => new
            {
                t.GroupId,
                GroupName = t.Group.Name
            })
            .FirstOrDefaultAsync();

        if (teacher == null)
        {
            return null;
        }

        var today = DateTime.Today;

        var childrenCount = await context.Children
            .CountAsync(c => !c.IsDeleted && c.GroupId == teacher.GroupId);

        var childrenWithMedicalRecordsCount = await context.Children
            .CountAsync(c =>
                !c.IsDeleted &&
                c.GroupId == teacher.GroupId &&
                c.MedicalRecords.Any(m => !m.IsDeleted));

        var childrenWithAllergiesCount = await context.Children
            .CountAsync(c =>
                !c.IsDeleted &&
                c.GroupId == teacher.GroupId &&
                c.MedicalRecords.Any(m =>
                    !m.IsDeleted &&
                    !string.IsNullOrWhiteSpace(m.Allergies)));

        var todayAttendance = await context.AttendanceRecords
            .Where(a => a.Date == today && a.Child.GroupId == teacher.GroupId)
            .ToListAsync();

        var upcomingEvents = await context.Events
            .Where(e =>
                !e.IsDeleted &&
                e.StartDateTime >= DateTime.Now &&
                (e.GroupId == null || e.GroupId == teacher.GroupId))
            .OrderBy(e => e.StartDateTime)
            .Take(5)
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

        var recentDailyReports = await context.DailyReports
            .Where(r =>
                !r.IsDeleted &&
                r.CreatedByUserId == userId)
            .OrderByDescending(r => r.ReportDate)
            .ThenBy(r => r.Child.FirstName)
            .ThenBy(r => r.Child.LastName)
            .Take(5)
            .Select(r => new TeacherDashboardDailyReportViewModel
            {
                Id = r.Id,
                ChildFullName = r.Child.FirstName + " " + r.Child.LastName,
                ReportDate = r.ReportDate,
                Mood = r.Mood
            })
            .ToListAsync();

        var recentAnnouncements = await context.Announcements
            .Where(a =>
                !a.IsDeleted &&
                (a.GroupId == null || a.GroupId == teacher.GroupId))
            .OrderByDescending(a => a.PublishedOn)
            .Take(5)
            .Select(a => new DashboardAnnouncementViewModel
            {
                Id = a.Id,
                Title = a.Title,
                PublishedOn = a.PublishedOn,
                GroupName = a.Group == null ? "All groups" : a.Group.Name
            })
            .ToListAsync();

        return new TeacherDashboardViewModel
        {
            GroupName = teacher.GroupName,
            ChildrenCount = childrenCount,
            PresentTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Present),
            AbsentTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent),
            SickTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Sick),
            LateTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Late),
            VacationTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Vacation),
            UpcomingEvents = upcomingEvents,
            RecentDailyReports = recentDailyReports,
            RecentAnnouncements = recentAnnouncements,
            ChildrenWithMedicalRecordsCount = childrenWithMedicalRecordsCount,
            ChildrenWithAllergiesCount = childrenWithAllergiesCount
        };
    }
}
