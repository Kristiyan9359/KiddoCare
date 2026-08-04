namespace KiddoCare.ViewModels.ActivityFeed;

public class ActivityFeedItemViewModel
{
    public DateTime Date { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ActionController { get; set; }

    public string? ActionName { get; set; }

    public int? RouteId { get; set; }
}