using KiddoCare.Data;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
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
}