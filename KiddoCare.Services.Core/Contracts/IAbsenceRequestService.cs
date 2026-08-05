using KiddoCare.ViewModels.AbsenceRequests;

namespace KiddoCare.Services.Core.Contracts;

public interface IAbsenceRequestService
{
    Task<IEnumerable<AbsenceRequestIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? statusFilter);

    Task<AbsenceRequestDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task<AbsenceRequestCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(AbsenceRequestCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<AbsenceRequestReviewViewModel?> GetForReviewAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task ReviewAsync(AbsenceRequestReviewViewModel model, string userId, bool isAdmin, bool isTeacher);
}