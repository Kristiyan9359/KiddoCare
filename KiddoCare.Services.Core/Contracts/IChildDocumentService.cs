using KiddoCare.ViewModels.ChildDocuments;

namespace KiddoCare.Services.Core.Contracts;

public interface IChildDocumentService
{
    Task<IEnumerable<ChildDocumentIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? statusFilter);

    Task<ChildDocumentDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task<ChildDocumentCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(ChildDocumentCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<ChildDocumentReviewViewModel?> GetForReviewAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task ReviewAsync(ChildDocumentReviewViewModel model, string userId, bool isAdmin, bool isTeacher);
}