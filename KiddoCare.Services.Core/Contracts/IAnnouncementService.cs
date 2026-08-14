using KiddoCare.ViewModels.Announcements;

namespace KiddoCare.Services.Core.Contracts;

public interface IAnnouncementService
{
    Task<AnnouncementListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, int page, int pageSize);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher);

    Task<AnnouncementDetailsViewModel?> GetDetailsAsync(int id);

    Task<bool> CanAccessAnnouncementAsync(int announcementId, string userId, bool isAdmin, bool isTeacher);

    Task<AnnouncementCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(AnnouncementCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<AnnouncementEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task EditAsync(AnnouncementEditViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);
}
