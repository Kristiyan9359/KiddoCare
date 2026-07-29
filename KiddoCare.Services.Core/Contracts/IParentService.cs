using KiddoCare.ViewModels.Parents;

namespace KiddoCare.Services.Core.Contracts;

public interface IParentService
{
    Task<IEnumerable<ParentIndexViewModel>> GetAllAsync();

    Task<ParentDetailsViewModel?> GetDetailsAsync(int id);

    Task<ParentCreateViewModel> GetCreateModelAsync();

    Task CreateAsync(ParentCreateViewModel model);

    Task<ParentEditViewModel?> GetForEditAsync(int id);

    Task EditAsync(ParentEditViewModel model);

    Task<ParentDeleteViewModel?> GetForDeleteAsync(int id);

    Task DeleteAsync(int id);
}