using KiddoCare.ViewModels.Events;

namespace KiddoCare.ViewModels.Dashboard;

public class ParentDashboardViewModel
{
    public int PendingConsentRequestsCount { get; set; }

    public int PendingChildDocumentsCount { get; set; }

    public IEnumerable<ParentDashboardChildViewModel> Children { get; set; } = new List<ParentDashboardChildViewModel>();

    public IEnumerable<EventIndexViewModel> UpcomingEvents { get; set; } = new List<EventIndexViewModel>();

    public IEnumerable<DashboardAnnouncementViewModel> RecentAnnouncements { get; set; } = new List<DashboardAnnouncementViewModel>();

    public IEnumerable<ParentDashboardAbsenceRequestViewModel> RecentAbsenceRequests { get; set; } = new List<ParentDashboardAbsenceRequestViewModel>();

    public IEnumerable<ParentDashboardConsentRequestViewModel> RecentConsentRequests { get; set; } = new List<ParentDashboardConsentRequestViewModel>();
}