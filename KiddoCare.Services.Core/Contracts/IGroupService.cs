using KiddoCare.ViewModels.Groups;

namespace KiddoCare.Services.Core.Contracts;

public interface IGroupService
{
    Task<GroupListViewModel> GetAllAsync(string? searchTerm, int page, int pageSize);

    Task<GroupEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(GroupCreateViewModel model);

    Task EditAsync(GroupEditViewModel model);

    Task<GroupDetailsViewModel?> GetDetailsAsync(int id);

    Task DeleteAsync(int id);

    Task<GroupDeleteViewModel?> GetForDeleteAsync(int id);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term);
}