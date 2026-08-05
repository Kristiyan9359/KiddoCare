using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Children;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KiddoCare.Services.Core;

public class ChildService : IChildService
{
    private readonly ApplicationDbContext context;

    public ChildService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<ChildIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? medicalFilter)
    {
        var query = context.Children
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            var teacherGroupId = await context.TeacherProfiles
                .Where(t => !t.IsDeleted && t.UserId == userId)
                .Select(t => (int?)t.GroupId)
                .FirstOrDefaultAsync();

            if (teacherGroupId == null)
            {
                return new List<ChildIndexViewModel>();
            }

            query = query.Where(c => c.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(c => c.Parent != null && c.Parent.UserId == userId);
        }

        if (medicalFilter == "with-records")
        {
            query = query.Where(c =>
                c.MedicalRecords.Any(m => !m.IsDeleted));
        }
        else if (medicalFilter == "with-allergies")
        {
            query = query.Where(c =>
                c.MedicalRecords.Any(m =>
                    !m.IsDeleted &&
                    !string.IsNullOrWhiteSpace(m.Allergies)));
        }

        return await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new ChildIndexViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                PhotoUrl = c.PhotoUrl,
                GroupName = c.Group.Name,
                HasMedicalRecord = c.MedicalRecords.Any(m => !m.IsDeleted),
                HasAllergies = c.MedicalRecords.Any(m =>
                    !m.IsDeleted &&
                    !string.IsNullOrWhiteSpace(m.Allergies))
            })
            .ToListAsync();
    }

    public async Task<ChildCreateViewModel> GetCreateModelAsync()
    {
        return new ChildCreateViewModel
        {
            Groups = await GetGroupSelectListAsync(),
            Parents = await GetParentSelectListAsync()
        };
    }

    public async Task<ChildEditViewModel?> GetForEditAsync(int id)
    {
        var child = await context.Children
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new ChildEditViewModel
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Gender = c.Gender,
                DateOfBirth = c.DateOfBirth,
                GroupId = c.GroupId,
                ParentId = c.ParentId,
                PhotoUrl = c.PhotoUrl
            })
            .FirstOrDefaultAsync();

        if (child == null)
        {
            return null;
        }

        child.Groups = await GetGroupSelectListAsync();

        child.Parents = await GetParentSelectListAsync();

        return child;
    }

    public async Task CreateAsync(ChildCreateViewModel model)
    {
        var child = new Child
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            GroupId = model.GroupId,
            ParentId = model.ParentId,
            PhotoUrl = model.PhotoUrl
        };

        await context.Children.AddAsync(child);
        await context.SaveChangesAsync();
    }

    public async Task EditAsync(ChildEditViewModel model)
    {
        var child = await context.Children
            .FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        child.FirstName = model.FirstName;
        child.LastName = model.LastName;
        child.Gender = model.Gender;
        child.DateOfBirth = model.DateOfBirth;
        child.GroupId = model.GroupId;
        child.ParentId = model.ParentId;
        child.PhotoUrl = model.PhotoUrl;

        await context.SaveChangesAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetParentSelectListAsync()
    {
        return await context.ParentProfiles
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.FullName + " (" + p.User.Email + ")"
            })
            .ToListAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var child = await context.Children
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        child.IsDeleted = true;

        await context.SaveChangesAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetGroupSelectListAsync()
    {
        return await context.KindergartenGroups
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync();
    }

    public async Task<ChildDetailsViewModel?> GetDetailsAsync(int id)
    {
        return await context.Children
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new ChildDetailsViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                GroupName = c.Group.Name,
                ParentName = c.Parent == null ? null : c.Parent.FullName,
                ParentEmail = c.Parent == null ? null : c.Parent.User.Email,
                ParentPhoneNumber = c.Parent == null ? null : c.Parent.PhoneNumber,
                PhotoUrl = c.PhotoUrl,
                HasMedicalRecord = c.MedicalRecords.Any(m => !m.IsDeleted),
                MedicalAllergies = c.MedicalRecords
                    .Where(m => !m.IsDeleted)
                    .Select(m => m.Allergies)
                    .FirstOrDefault(),
                MedicalChronicConditions = c.MedicalRecords
                    .Where(m => !m.IsDeleted)
                    .Select(m => m.ChronicConditions)
                    .FirstOrDefault(),
                MedicalEmergencyContactName = c.MedicalRecords
                    .Where(m => !m.IsDeleted)
                    .Select(m => m.EmergencyContactName)
                    .FirstOrDefault(),
                MedicalEmergencyContactPhone = c.MedicalRecords
                    .Where(m => !m.IsDeleted)
                    .Select(m => m.EmergencyContactPhone)
                    .FirstOrDefault(),
                RecentAbsenceRequests = c.AbsenceRequests
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.RequestedOn)
                    .Take(3)
                    .Select(a => new ChildDetailsAbsenceRequestViewModel
                    {
                        Id = a.Id,
                        StartDate = a.StartDate,
                        EndDate = a.EndDate,
                        Reason = a.Reason,
                        Status = a.Status
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ChildDeleteViewModel?> GetForDeleteAsync(int id)
    {
        return await context.Children
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new ChildDeleteViewModel
            {
                Id = c.Id,
                FullName = c.FirstName + " " + c.LastName,
                GroupName = c.Group.Name,
                DateOfBirth = c.DateOfBirth
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanAccessChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await context.Children
                .AnyAsync(c => c.Id == childId && !c.IsDeleted);
        }

        if (isTeacher)
        {
            var teacherGroupId = await context.TeacherProfiles
                .Where(t => !t.IsDeleted && t.UserId == userId)
                .Select(t => (int?)t.GroupId)
                .FirstOrDefaultAsync();

            if (teacherGroupId == null)
            {
                return false;
            }

            return await context.Children
                .AnyAsync(c =>
                    c.Id == childId &&
                    !c.IsDeleted &&
                    c.GroupId == teacherGroupId.Value);
        }

        return await context.Children
            .AnyAsync(c =>
                c.Id == childId &&
                !c.IsDeleted &&
                c.Parent != null &&
                c.Parent.UserId == userId);
    }
}
