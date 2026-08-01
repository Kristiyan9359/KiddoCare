using KiddoCare.ViewModels.Events;

namespace KiddoCare.ViewModels.Dashboard;

public class ParentDashboardViewModel
{
    public IEnumerable<ParentDashboardChildViewModel> Children { get; set; } = new List<ParentDashboardChildViewModel>();

    public IEnumerable<EventIndexViewModel> UpcomingEvents { get; set; } = new List<EventIndexViewModel>();

    public IEnumerable<DashboardAnnouncementViewModel> RecentAnnouncements { get; set; } = new List<DashboardAnnouncementViewModel>();
}