using KiddoCare.ViewModels.Groups;

namespace KiddoCare.Services.Core.Contracts;

public interface IGroupService
{
    Task<IEnumerable<GroupIndexViewModel>> GetAllAsync();

    Task<GroupEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(GroupCreateViewModel model);

    Task EditAsync(GroupEditViewModel model);

    Task DeleteAsync(int id);
}