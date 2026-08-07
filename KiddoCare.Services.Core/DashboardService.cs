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

        var pendingAbsenceRequestsCount = await context.AbsenceRequests
            .CountAsync(a => !a.IsDeleted && a.Status == RequestStatus.Pending);

        var pendingConsentRequestsCount = await context.ConsentRequests
            .CountAsync(c => !c.IsDeleted && c.Status == RequestStatus.Pending);

        var pendingChildDocumentsCount = await context.ChildDocuments
            .CountAsync(d => !d.IsDeleted && d.Status == RequestStatus.Pending);

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

        var recentDocuments = await context.ChildDocuments
            .Where(d => !d.IsDeleted && !d.Child.IsDeleted)
            .OrderByDescending(d => d.UploadedOn)
            .Take(5)
            .Select(d => new DashboardDocumentViewModel
            {
                Id = d.Id,
                ChildFullName = d.Child.FirstName + " " + d.Child.LastName,
                GroupName = d.Child.Group.Name,
                Type = d.Type,
                Title = d.Title,
                Status = d.Status,
                UploadedOn = d.UploadedOn
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
            RecentAnnouncements = recentAnnouncements,
            PendingAbsenceRequestsCount = pendingAbsenceRequestsCount,
            PendingConsentRequestsCount = pendingConsentRequestsCount,
            PendingChildDocumentsCount = pendingChildDocumentsCount,
            RecentDocuments = recentDocuments
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

        var recentAbsenceRequests = await context.AbsenceRequests
            .Where(a =>
                !a.IsDeleted &&
                a.Status != RequestStatus.Rejected &&
                a.Child.Parent != null &&
                !a.Child.Parent.IsDeleted &&
                a.Child.Parent.UserId == userId)
            .OrderByDescending(a => a.RequestedOn)
            .Take(5)
            .Select(a => new ParentDashboardAbsenceRequestViewModel
            {
                Id = a.Id,
                ChildFullName = a.Child.FirstName + " " + a.Child.LastName,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Reason = a.Reason,
                Status = a.Status
            })
            .ToListAsync();

        var pendingConsentRequestsCount = await context.ConsentRequests
            .CountAsync(c =>
                !c.IsDeleted &&
                c.Status == RequestStatus.Pending &&
                c.Child.Parent != null &&
                !c.Child.Parent.IsDeleted &&
                c.Child.Parent.UserId == userId);

        var pendingChildDocumentsCount = await context.ChildDocuments
            .CountAsync(d =>
                !d.IsDeleted &&
                d.Status == RequestStatus.Pending &&
                d.Child.Parent != null &&
                !d.Child.Parent.IsDeleted &&
                d.Child.Parent.UserId == userId);

        var recentConsentRequests = await context.ConsentRequests
             .Where(c =>
                 !c.IsDeleted &&
                 c.Child.Parent != null &&
                 !c.Child.Parent.IsDeleted &&
                 c.Child.Parent.UserId == userId)
             .OrderByDescending(c => c.CreatedOn)
             .Take(5)
             .Select(c => new ParentDashboardConsentRequestViewModel
             {
                 Id = c.Id,
                 ChildFullName = c.Child.FirstName + " " + c.Child.LastName,
                 Title = c.Title,
                 Type = c.Type,
                 Status = c.Status,
                 CreatedOn = c.CreatedOn,
                 CanRespond = c.Status == RequestStatus.Pending
             })
             .ToListAsync();

        var recentDocuments = await context.ChildDocuments
             .Where(d =>
                 !d.IsDeleted &&
                 d.Child.Parent != null &&
                 !d.Child.Parent.IsDeleted &&
                 d.Child.Parent.UserId == userId)
             .OrderByDescending(d => d.UploadedOn)
             .Take(5)
             .Select(d => new ParentDashboardDocumentViewModel
             {
                 Id = d.Id,
                 ChildFullName = d.Child.FirstName + " " + d.Child.LastName,
                 Type = d.Type,
                 Title = d.Title,
                 Status = d.Status,
                 UploadedOn = d.UploadedOn
             })
             .ToListAsync();

        return new ParentDashboardViewModel
        {
            Children = children,
            UpcomingEvents = upcomingEvents,
            RecentAnnouncements = recentAnnouncements,
            RecentAbsenceRequests = recentAbsenceRequests,
            PendingConsentRequestsCount = pendingConsentRequestsCount,
            RecentConsentRequests = recentConsentRequests,
            PendingChildDocumentsCount = pendingChildDocumentsCount,
            RecentDocuments = recentDocuments
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

        var pendingAbsenceRequestsCount = await context.AbsenceRequests
            .CountAsync(a =>
                !a.IsDeleted &&
                a.Status == RequestStatus.Pending &&
                a.Child.GroupId == teacher.GroupId);


        var pendingConsentRequestsCount = await context.ConsentRequests
            .CountAsync(c =>
                !c.IsDeleted &&
                c.Status == RequestStatus.Pending &&
                c.Child.GroupId == teacher.GroupId);

        var pendingChildDocumentsCount = await context.ChildDocuments
            .CountAsync(d =>
                !d.IsDeleted &&
                d.Status == RequestStatus.Pending &&
                d.Child.GroupId == teacher.GroupId);

        var childrenCount = await context.Children
            .CountAsync(c => !c.IsDeleted && c.GroupId == teacher.GroupId);

        var today = DateTime.Today;

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

        var recentDocuments = await context.ChildDocuments
           .Where(d =>
               !d.IsDeleted &&
               d.Child.GroupId == teacher.GroupId)
           .OrderByDescending(d => d.UploadedOn)
           .Take(5)
           .Select(d => new TeacherDashboardDocumentViewModel
           {
               Id = d.Id,
               ChildFullName = d.Child.FirstName + " " + d.Child.LastName,
               Type = d.Type,
               Title = d.Title,
               Status = d.Status,
               UploadedOn = d.UploadedOn
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
            ChildrenWithAllergiesCount = childrenWithAllergiesCount,
            PendingAbsenceRequestsCount = pendingAbsenceRequestsCount,
            PendingConsentRequestsCount = pendingConsentRequestsCount,
            PendingChildDocumentsCount = pendingChildDocumentsCount,
            RecentDocuments = recentDocuments
        };
    }
}
