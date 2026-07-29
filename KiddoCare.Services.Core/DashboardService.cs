using KiddoCare.Data;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Children;
using KiddoCare.ViewModels.Dashboard;
using KiddoCare.ViewModels.Events;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

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

        return new DashboardViewModel
        {
            GroupsCount = groupsCount,
            ChildrenCount = childrenCount,
            PresentTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Present),
            AbsentTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent),
            SickTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Sick),
            LateTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Late),
            VacationTodayCount = todayAttendance.Count(a => a.Status == AttendanceStatus.Vacation),
            UpcomingEvents = upcomingEvents
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
            .Select(c => new ChildIndexViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                PhotoUrl = c.PhotoUrl,
                GroupName = c.Group.Name
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

        return new ParentDashboardViewModel
        {
            Children = children,
            UpcomingEvents = upcomingEvents
        };
    }
}