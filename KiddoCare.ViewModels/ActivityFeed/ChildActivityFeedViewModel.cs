namespace KiddoCare.ViewModels.ActivityFeed;

public class ChildActivityFeedViewModel
{
    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public IEnumerable<ActivityFeedItemViewModel> Items { get; set; } = new List<ActivityFeedItemViewModel>();
}