using KiddoCare.ViewModels.Children;

namespace KiddoCare.Services.Core.Contracts;

public interface IChildService
{
    Task<ChildListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? medicalFilter, int page, int pageSize);

    Task<ChildCreateViewModel> GetCreateModelAsync();

    Task<ChildEditViewModel?> GetForEditAsync(int id);

    Task CreateAsync(ChildCreateViewModel model);

    Task EditAsync(ChildEditViewModel model);

    Task DeleteAsync(int id);

    Task<ChildDetailsViewModel?> GetDetailsAsync(int id);

    Task<ChildDeleteViewModel?> GetForDeleteAsync(int id);

    Task<bool> CanAccessChildAsync(int childId, string userId, bool isAdmin, bool isTeacher);
}