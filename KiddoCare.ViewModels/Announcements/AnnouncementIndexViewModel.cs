namespace KiddoCare.ViewModels.Announcements;

public class AnnouncementIndexViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string ContentPreview { get; set; } = null!;

    public string GroupName { get; set; } = "All groups";

    public DateTime PublishedOn { get; set; }

    public bool CanManage { get; set; }
}