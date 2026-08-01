using KiddoCare.ViewModels.MedicalRecords;

namespace KiddoCare.Services.Core.Contracts;

public interface IMedicalRecordService
{
    Task<MedicalRecordDetailsViewModel?> GetDetailsAsync(int childId, string userId, bool isAdmin, bool isTeacher);

    Task<MedicalRecordCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher);

    Task CreateAsync(MedicalRecordCreateViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<MedicalRecordEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task EditAsync(MedicalRecordEditViewModel model, string userId, bool isAdmin, bool isTeacher);

    Task<MedicalRecordDeleteViewModel?> GetForDeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);

    Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher);
}