using KiddoCare.ViewModels.Dashboard;

namespace KiddoCare.Services.Core.Contracts;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();
}