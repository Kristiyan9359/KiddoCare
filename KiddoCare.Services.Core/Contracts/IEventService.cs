using KiddoCare.ViewModels.Events;

namespace KiddoCare.Services.Core.Contracts;

public interface IEventService
{
    Task<IEnumerable<EventIndexViewModel>> GetAllAsync();

    Task<EventDetailsViewModel?> GetDetailsAsync(int id);

    Task<EventCreateViewModel> GetCreateModelAsync();

    Task<EventEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(EventCreateViewModel model);

    Task EditAsync(EventEditViewModel model);

    Task DeleteAsync(int id);
}