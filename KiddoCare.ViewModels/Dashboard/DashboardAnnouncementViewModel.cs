namespace KiddoCare.ViewModels.Dashboard;

public class DashboardAnnouncementViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public DateTime PublishedOn { get; set; }

    public string GroupName { get; set; } = null!;
}