using KiddoCare.ViewModels.Teachers;

namespace KiddoCare.Services.Core.Contracts;

public interface ITeacherService
{
    Task<TeacherListViewModel> GetAllAsync(string? searchTerm, int page, int pageSize);

    Task<TeacherDetailsViewModel?> GetDetailsAsync(int id);

    Task<TeacherCreateViewModel> GetCreateModelAsync();

    Task CreateAsync(TeacherCreateViewModel model);

    Task<TeacherEditViewModel?> GetForEditAsync(int id);

    Task EditAsync(TeacherEditViewModel model);

    Task<TeacherDeleteViewModel?> GetForDeleteAsync(int id);

    Task DeleteAsync(int id);

    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term);
}