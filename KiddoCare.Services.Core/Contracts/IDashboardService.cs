using KiddoCare.ViewModels.Dashboard;

namespace KiddoCare.Services.Core.Contracts;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();

    Task<ParentDashboardViewModel> GetParentDashboardAsync(string userId);
}