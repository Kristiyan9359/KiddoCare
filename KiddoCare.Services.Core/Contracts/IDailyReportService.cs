namespace KiddoCare.Services.Core.Contracts;

using KiddoCare.ViewModels.DailyReports;

public interface IDailyReportService
{
    Task<DailyReportListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, int page, int pageSize);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher);

    Task<bool> CanAccessAsync(int dailyReportId, string userId, bool isAdmin, bool isTeacher);

    Task<DailyReportDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task<DailyReportCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(DailyReportCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<DailyReportEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task EditAsync(DailyReportEditViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<DailyReportDeleteViewModel?> GetForDeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);
}
