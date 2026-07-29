using KiddoCare.ViewModels.Children;

namespace KiddoCare.Services.Core.Contracts;

public interface IChildService
{
    Task<IEnumerable<ChildIndexViewModel>> GetAllAsync(string userId, bool isAdminOrTeacher);

    Task<ChildCreateViewModel> GetCreateModelAsync();

    Task<ChildEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(ChildCreateViewModel model);

    Task EditAsync(ChildEditViewModel model);

    Task DeleteAsync(int id);

    Task<ChildDetailsViewModel?> GetDetailsAsync(int id);

    Task<ChildDeleteViewModel?> GetForDeleteAsync(int id);
}