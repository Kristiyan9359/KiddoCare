using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.MedicalRecords;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly ApplicationDbContext context;

    public MedicalRecordService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<MedicalRecordDetailsViewModel?> GetDetailsAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        var canAccess = await CanAccessChildAsync(childId, userId, isAdmin, isTeacher);

        if (!canAccess)
        {
            return null;
        }

        return await context.MedicalRecords
            .Where(m => !m.IsDeleted && m.ChildId == childId)
            .Select(m => new MedicalRecordDetailsViewModel
            {
                Id = m.Id,
                ChildId = m.ChildId,
                ChildFullName = m.Child.FirstName + " " + m.Child.LastName,
                Allergies = m.Allergies,
                ChronicConditions = m.ChronicConditions,
                DoctorName = m.DoctorName,
                DoctorPhone = m.DoctorPhone,
                EmergencyContactName = m.EmergencyContactName,
                EmergencyContactPhone = m.EmergencyContactPhone,
                Notes = m.Notes,
                CanManage = isAdmin
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MedicalRecordCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new MedicalRecordCreateViewModel
        {
            Children = await GetChildrenSelectListAsync()
        };
    }

    public async Task CreateAsync(MedicalRecordCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            throw new InvalidOperationException("Medical record not found.");
        }

        if (!model.ChildId.HasValue)
        {
            throw new InvalidOperationException("Child is required.");
        }

        var childExists = await context.Children
            .AnyAsync(c => !c.IsDeleted && c.Id == model.ChildId.Value);

        if (!childExists)
        {
            throw new InvalidOperationException("Child not found.");
        }

        var alreadyExists = await context.MedicalRecords
            .AnyAsync(m => !m.IsDeleted && m.ChildId == model.ChildId.Value);

        if (alreadyExists)
        {
            throw new InvalidOperationException("Medical record already exists for this child.");
        }

        var medicalRecord = new MedicalRecord
        {
            ChildId = model.ChildId.Value,
            Allergies = model.Allergies,
            ChronicConditions = model.ChronicConditions,
            DoctorName = model.DoctorName,
            DoctorPhone = model.DoctorPhone,
            EmergencyContactName = model.EmergencyContactName,
            EmergencyContactPhone = model.EmergencyContactPhone,
            Notes = model.Notes
        };

        await context.MedicalRecords.AddAsync(medicalRecord);
        await context.SaveChangesAsync();
    }

    public async Task<MedicalRecordEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            return null;
        }

        return await context.MedicalRecords
            .Where(m => !m.IsDeleted && m.Id == id)
            .Select(m => new MedicalRecordEditViewModel
            {
                Id = m.Id,
                ChildFullName = m.Child.FirstName + " " + m.Child.LastName,
                Allergies = m.Allergies,
                ChronicConditions = m.ChronicConditions,
                DoctorName = m.DoctorName,
                DoctorPhone = m.DoctorPhone,
                EmergencyContactName = m.EmergencyContactName,
                EmergencyContactPhone = m.EmergencyContactPhone,
                Notes = m.Notes,
                ChildId = m.ChildId
            })
            .FirstOrDefaultAsync();
    }

    public async Task EditAsync(MedicalRecordEditViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            throw new InvalidOperationException("Medical record not found.");
        }

        var medicalRecord = await context.MedicalRecords
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == model.Id);

        if (medicalRecord == null)
        {
            throw new InvalidOperationException("Medical record not found.");
        }

        medicalRecord.Allergies = model.Allergies;
        medicalRecord.ChronicConditions = model.ChronicConditions;
        medicalRecord.DoctorName = model.DoctorName;
        medicalRecord.DoctorPhone = model.DoctorPhone;
        medicalRecord.EmergencyContactName = model.EmergencyContactName;
        medicalRecord.EmergencyContactPhone = model.EmergencyContactPhone;
        medicalRecord.Notes = model.Notes;

        await context.SaveChangesAsync();
    }

    public async Task<MedicalRecordDeleteViewModel?> GetForDeleteAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            return null;
        }

        return await context.MedicalRecords
            .Where(m => !m.IsDeleted && m.Id == id)
            .Select(m => new MedicalRecordDeleteViewModel
            {
                Id = m.Id,
                ChildFullName = m.Child.FirstName + " " + m.Child.LastName
            })
            .FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        if (!isAdmin)
        {
            throw new InvalidOperationException("Medical record not found.");
        }

        var medicalRecord = await context.MedicalRecords
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == id);

        if (medicalRecord == null)
        {
            throw new InvalidOperationException("Medical record not found.");
        }

        medicalRecord.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    private async Task<bool> CanAccessChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Children
                .AnyAsync(c => !c.IsDeleted && c.Id == childId);
        }

        if (isTeacher)
        {
            var teacherGroupId = await context.TeacherProfiles
                .Where(t => !t.IsDeleted && t.UserId == userId)
                .Select(t => (int?)t.GroupId)
                .FirstOrDefaultAsync();

            return teacherGroupId.HasValue &&
                   await context.Children.AnyAsync(c =>
                       !c.IsDeleted &&
                       c.Id == childId &&
                       c.GroupId == teacherGroupId.Value);
        }

        return await context.Children.AnyAsync(c =>
            !c.IsDeleted &&
            c.Id == childId &&
            c.Parent != null &&
            !c.Parent.IsDeleted &&
            c.Parent.UserId == userId);
    }

    private async Task<IEnumerable<SelectListItem>> GetChildrenSelectListAsync()
    {
        return await context.Children
            .Where(c =>
                !c.IsDeleted &&
                !c.MedicalRecords.Any(m => !m.IsDeleted))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FirstName + " " + c.LastName
            })
            .ToListAsync();
    }
}
