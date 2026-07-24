using KiddoCare.ViewModels.Children;

namespace KiddoCare.Services.Core.Contracts;

public interface IChildService
{
    Task<IEnumerable<ChildIndexViewModel>> GetAllAsync();

    Task<ChildCreateViewModel> GetCreateModelAsync();

    Task<ChildEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(ChildCreateViewModel model);

    Task EditAsync(ChildEditViewModel model);

    Task DeleteAsync(int id);
}