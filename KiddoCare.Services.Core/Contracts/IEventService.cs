using KiddoCare.ViewModels.Events;

namespace KiddoCare.Services.Core.Contracts;

public interface IEventService
{
    Task<IEnumerable<EventIndexViewModel>> GetAllAsync(string userId, bool isAdminOrTeacher);

    Task<EventDetailsViewModel?> GetDetailsAsync(int id);

    Task<bool> CanAccessEventAsync(int eventId, string userId, bool isAdminOrTeacher);

    Task<EventCreateViewModel> GetCreateModelAsync();

    Task<EventEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(EventCreateViewModel model);

    Task EditAsync(EventEditViewModel model);

    Task DeleteAsync(int id);
}