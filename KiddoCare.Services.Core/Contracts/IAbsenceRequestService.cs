using KiddoCare.ViewModels.AbsenceRequests;

namespace KiddoCare.Services.Core.Contracts;

public interface IAbsenceRequestService
{
    Task<AbsenceRequestListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, string? statusFilter, int page, int pageSize);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher);

    Task<AbsenceRequestDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task<AbsenceRequestCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(AbsenceRequestCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<AbsenceRequestReviewViewModel?> GetForReviewAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task ReviewAsync(AbsenceRequestReviewViewModel model, string userId, bool isAdmin, bool isTeacher);
}
