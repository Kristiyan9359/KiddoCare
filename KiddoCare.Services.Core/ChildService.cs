using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
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


    public async Task<ChildListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, string? medicalFilter, int page, int pageSize)
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
                return new ChildListViewModel
                {
                    SearchTerm = searchTerm,
                    MedicalRecordsFilter = medicalFilter,
                    Page = 1,
                    PageSize = pageSize
                };
            }

            query = query.Where(c => c.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(c => c.Parent != null && c.Parent.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(c =>
                (c.FirstName + " " + c.LastName).Contains(searchTerm) ||
                c.Group.Name.Contains(searchTerm) ||
                (c.Parent != null && c.Parent.FullName.Contains(searchTerm)) ||
                (c.Parent != null && c.Parent.User.Email!.Contains(searchTerm)));
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

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalChildren = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalChildren / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var children = await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new ChildListViewModel
        {
            Children = children,
            SearchTerm = searchTerm,
            MedicalRecordsFilter = medicalFilter,
            Page = page,
            PageSize = pageSize,
            TotalChildren = totalChildren
        };
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
                    .Where(a => !a.IsDeleted && a.Status != RequestStatus.Rejected)
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
                    .ToList(),
                RecentConsentRequests = c.ConsentRequests
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.CreatedOn)
                    .Take(3)
                    .Select(r => new ChildDetailsConsentRequestViewModel
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Type = r.Type,
                        Status = r.Status,
                        CreatedOn = r.CreatedOn
                    })
                    .ToList(),
                RecentDocuments = c.ChildDocuments
                    .Where(d => !d.IsDeleted)
                    .OrderByDescending(d => d.UploadedOn)
                    .Take(3)
                    .Select(d => new ChildDetailsDocumentViewModel
                    {
                        Id = d.Id,
                        Type = d.Type,
                        Title = d.Title,
                        Status = d.Status,
                        UploadedOn = d.UploadedOn
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

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

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
                return new List<string>();
            }

            query = query.Where(c => c.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(c => c.Parent != null && c.Parent.UserId == userId);
        }

        return await query
            .Where(c =>
                (c.FirstName + " " + c.LastName).Contains(term) ||
                c.Group.Name.Contains(term) ||
                (c.Parent != null && c.Parent.FullName.Contains(term)) ||
                (c.Parent != null && c.Parent.User.Email!.Contains(term)))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => c.FirstName + " " + c.LastName)
            .Distinct()
            .Take(8)
            .ToListAsync();
    }
}
