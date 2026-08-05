using KiddoCare.ViewModels.ConsentRequests;

namespace KiddoCare.Services.Core.Contracts;

public interface IConsentRequestService
{
    Task<IEnumerable<ConsentRequestIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? statusFilter);

    Task<ConsentRequestDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task<ConsentRequestCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(ConsentRequestCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<ConsentRequestRespondViewModel?> GetForRespondAsync(int id, string userId);

    Task RespondAsync(ConsentRequestRespondViewModel model, string userId);
}