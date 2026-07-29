using KiddoCare.ViewModels.Children;
using KiddoCare.ViewModels.Events;

namespace KiddoCare.ViewModels.Dashboard;

public class ParentDashboardViewModel
{
    public IEnumerable<ChildIndexViewModel> Children { get; set; } = new List<ChildIndexViewModel>();

    public IEnumerable<EventIndexViewModel> UpcomingEvents { get; set; } = new List<EventIndexViewModel>();
}