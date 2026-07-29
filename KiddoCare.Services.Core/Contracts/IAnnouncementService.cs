using KiddoCare.ViewModels.Announcements;

namespace KiddoCare.Services.Core.Contracts;

public interface IAnnouncementService
{
    Task<IEnumerable<AnnouncementIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher);

    Task<AnnouncementDetailsViewModel?> GetDetailsAsync(int id);

    Task<bool> CanAccessAnnouncementAsync(int announcementId, string userId, bool isAdmin, bool isTeacher);

    Task<AnnouncementCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(AnnouncementCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<AnnouncementEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task EditAsync(AnnouncementEditViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);
}