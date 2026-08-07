using KiddoCare.ViewModels.Events;

namespace KiddoCare.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int GroupsCount { get; set; }

    public int ChildrenCount { get; set; }

    public int PresentTodayCount { get; set; }

    public int AbsentTodayCount { get; set; }

    public int SickTodayCount { get; set; }

    public int LateTodayCount { get; set; }

    public int VacationTodayCount { get; set; }

    public int PendingAbsenceRequestsCount { get; set; }

    public int PendingConsentRequestsCount { get; set; }

    public int PendingChildDocumentsCount { get; set; }

    public IEnumerable<EventIndexViewModel> UpcomingEvents { get; set; } = new List<EventIndexViewModel>();

    public IEnumerable<DashboardAnnouncementViewModel> RecentAnnouncements { get; set; } = new List<DashboardAnnouncementViewModel>();

    public IEnumerable<DashboardDocumentViewModel> RecentDocuments { get; set; } = new List<DashboardDocumentViewModel>();
}