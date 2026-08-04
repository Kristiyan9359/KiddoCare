using KiddoCare.ViewModels.ActivityFeed;

namespace KiddoCare.Services.Core.Contracts;

public interface IActivityFeedService
{
    Task<ChildActivityFeedViewModel?> GetChildFeedAsync(int childId, string userId, bool isAdmin, bool isTeacher);
}