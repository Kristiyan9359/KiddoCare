using KiddoCare.ViewModels.Events;

namespace KiddoCare.Services.Core.Contracts;

public interface IEventService
{
    Task<EventListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, int page, int pageSize);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher);

    Task<EventDetailsViewModel?> GetDetailsAsync(int id);

    Task<bool> CanAccessEventAsync(int eventId, string userId, bool isAdmin, bool isTeacher);

    Task<EventCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(EventCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<EventEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task EditAsync(EventEditViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);
}
