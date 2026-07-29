namespace KiddoCare.ViewModels.Announcements;

public class AnnouncementDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string GroupName { get; set; } = "All groups";

    public DateTime PublishedOn { get; set; }

    public bool IsPublic { get; set; }
}