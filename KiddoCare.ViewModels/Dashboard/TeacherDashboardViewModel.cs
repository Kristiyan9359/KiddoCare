using KiddoCare.ViewModels.Events;

namespace KiddoCare.ViewModels.Dashboard;

public class TeacherDashboardViewModel
{
    public string GroupName { get; set; } = null!;

    public int ChildrenCount { get; set; }

    public int PresentTodayCount { get; set; }

    public int AbsentTodayCount { get; set; }

    public int SickTodayCount { get; set; }

    public int LateTodayCount { get; set; }

    public int VacationTodayCount { get; set; }

    public IEnumerable<EventIndexViewModel> UpcomingEvents { get; set; } = new List<EventIndexViewModel>();

    public IEnumerable<TeacherDashboardDailyReportViewModel> RecentDailyReports { get; set; } = new List<TeacherDashboardDailyReportViewModel>();

    public IEnumerable<DashboardAnnouncementViewModel> RecentAnnouncements { get; set; } = new List<DashboardAnnouncementViewModel>();
}