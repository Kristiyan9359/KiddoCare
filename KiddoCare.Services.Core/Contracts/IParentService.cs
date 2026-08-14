using KiddoCare.ViewModels.Parents;

namespace KiddoCare.Services.Core.Contracts;

public interface IParentService
{
    Task<ParentListViewModel> GetAllAsync(string? searchTerm, int page, int pageSize);

    Task<ParentDetailsViewModel?> GetDetailsAsync(int id);

    Task<ParentCreateViewModel> GetCreateModelAsync();

    Task CreateAsync(ParentCreateViewModel model);

    Task<ParentEditViewModel?> GetForEditAsync(int id);

    Task EditAsync(ParentEditViewModel model);

    Task<ParentDeleteViewModel?> GetForDeleteAsync(int id);

    Task DeleteAsync(int id);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term);
}