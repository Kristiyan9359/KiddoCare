namespace KiddoCare.Services.Core;

using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.DailyReports;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class DailyReportService : IDailyReportService
{
    private readonly ApplicationDbContext context;

    public DailyReportService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<DailyReportIndexViewModel>> GetAllAsync(string userId, bool isAdmin, bool isTeacher)
    {
        var query = this.context.DailyReports
            .Where(r => !r.IsDeleted && !r.Child.IsDeleted)
            .AsQueryable();

        int? teacherGroupId = null;

        if (isTeacher && !isAdmin)
        {
            teacherGroupId = await this.GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<DailyReportIndexViewModel>();
            }

            query = query.Where(r => r.Child.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            query = query.Where(r =>
                r.Child.Parent != null &&
                !r.Child.Parent.IsDeleted &&
                r.Child.Parent.UserId == userId);
        }

        return await query
            .OrderByDescending(r => r.ReportDate)
            .ThenBy(r => r.Child.FirstName)
            .ThenBy(r => r.Child.LastName)
            .Select(r => new DailyReportIndexViewModel
            {
                Id = r.Id,
                ChildId = r.ChildId,
                ChildFullName = r.Child.FirstName + " " + r.Child.LastName,
                ReportDate = r.ReportDate,
                Mood = r.Mood,
                CanManage = isAdmin || (isTeacher && r.CreatedByUserId == userId)
            })
            .ToListAsync();
    }

    public async Task<bool> CanAccessAsync(int dailyReportId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await this.context.DailyReports
                .AnyAsync(r => r.Id == dailyReportId && !r.IsDeleted);
        }

        if (isTeacher)
        {
            int? teacherGroupId = await this.GetTeacherGroupIdAsync(userId);

            return teacherGroupId.HasValue &&
                   await this.context.DailyReports.AnyAsync(r =>
                       r.Id == dailyReportId &&
                       !r.IsDeleted &&
                       !r.Child.IsDeleted &&
                       r.Child.GroupId == teacherGroupId.Value);
        }

        return await this.context.DailyReports.AnyAsync(r =>
            r.Id == dailyReportId &&
            !r.IsDeleted &&
            !r.Child.IsDeleted &&
            r.Child.Parent != null &&
            !r.Child.Parent.IsDeleted &&
            r.Child.Parent.UserId == userId);
    }

    public async Task<DailyReportDetailsViewModel?> GetDetailsAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        DailyReportDetailsViewModel? model = await this.context.DailyReports
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(r => new DailyReportDetailsViewModel
            {
                Id = r.Id,
                ChildId = r.ChildId,
                ChildFullName = r.Child.FirstName + " " + r.Child.LastName,
                ReportDate = r.ReportDate,
                Mood = r.Mood,
                Meals = r.Meals,
                Sleep = r.Sleep,
                Activities = r.Activities,
                TeacherNote = r.TeacherNote,
                CreatedOn = r.CreatedOn
            })
            .FirstOrDefaultAsync();

        if (model == null)
        {
            return null;
        }

        model.CanManage = await this.CanManageAsync(id, userId, isAdmin, isTeacher);

        return model;
    }

    public async Task<DailyReportCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher)
    {
        return new DailyReportCreateViewModel
        {
            Children = await this.GetChildrenSelectListAsync(userId, isAdmin, isTeacher)
        };
    }

    public async Task CreateAsync(DailyReportCreateViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (!model.ChildId.HasValue)
        {
            throw new InvalidOperationException("Child is required.");
        }

        if (model.Mood == ChildMood.Unknown)
        {
            throw new InvalidOperationException("Child mood is required.");
        }

        if (model.ReportDate.Date > DateTime.Today)
        {
            throw new InvalidOperationException("A daily report cannot be created for a future date.");
        }

        bool canManageChild = await this.CanManageChildAsync(
            model.ChildId.Value,
            userId,
            isAdmin,
            isTeacher);

        if (!canManageChild)
        {
            throw new InvalidOperationException("Child not found.");
        }

        bool alreadyExists = await this.context.DailyReports.AnyAsync(r =>
            !r.IsDeleted &&
            r.ChildId == model.ChildId.Value &&
            r.ReportDate == model.ReportDate.Date);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                "A daily report for this child and date already exists.");
        }

        DailyReport dailyReport = new DailyReport
        {
            ChildId = model.ChildId.Value,
            ReportDate = model.ReportDate.Date,
            Mood = model.Mood,
            Meals = model.Meals,
            Sleep = model.Sleep,
            Activities = model.Activities,
            TeacherNote = model.TeacherNote,
            CreatedByUserId = userId
        };

        await this.context.DailyReports.AddAsync(dailyReport);
        await this.context.SaveChangesAsync();
    }

    public async Task<DailyReportEditViewModel?> GetForEditAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        bool canManage = await this.CanManageAsync(id, userId, isAdmin, isTeacher);

        if (!canManage)
        {
            return null;
        }

        return await this.context.DailyReports
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(r => new DailyReportEditViewModel
            {
                Id = r.Id,
                ChildFullName = r.Child.FirstName + " " + r.Child.LastName,
                ReportDate = r.ReportDate,
                Mood = r.Mood,
                Meals = r.Meals,
                Sleep = r.Sleep,
                Activities = r.Activities,
                TeacherNote = r.TeacherNote
            })
            .FirstOrDefaultAsync();
    }

    public async Task EditAsync(DailyReportEditViewModel model, string userId, bool isAdmin, bool isTeacher)
    {
        if (model.Mood == ChildMood.Unknown)
        {
            throw new InvalidOperationException("Child mood is required.");
        }

        if (model.ReportDate.Date > DateTime.Today)
        {
            throw new InvalidOperationException("A daily report cannot be created for a future date.");
        }

        bool canManage = await this.CanManageAsync(model.Id, userId, isAdmin, isTeacher);

        if (!canManage)
        {
            throw new InvalidOperationException("Daily report not found.");
        }

        DailyReport? dailyReport = await this.context.DailyReports
            .FirstOrDefaultAsync(r => r.Id == model.Id && !r.IsDeleted);

        if (dailyReport == null)
        {
            throw new InvalidOperationException("Daily report not found.");
        }

        bool alreadyExists = await this.context.DailyReports.AnyAsync(r =>
            r.Id != model.Id &&
            !r.IsDeleted &&
            r.ChildId == dailyReport.ChildId &&
            r.ReportDate == model.ReportDate.Date);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                "A daily report for this child and date already exists.");
        }

        dailyReport.ReportDate = model.ReportDate.Date;
        dailyReport.Mood = model.Mood;
        dailyReport.Meals = model.Meals;
        dailyReport.Sleep = model.Sleep;
        dailyReport.Activities = model.Activities;
        dailyReport.TeacherNote = model.TeacherNote;

        await this.context.SaveChangesAsync();
    }

    public async Task<DailyReportDeleteViewModel?> GetForDeleteAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        bool canManage = await this.CanManageAsync(id, userId, isAdmin, isTeacher);

        if (!canManage)
        {
            return null;
        }

        DailyReport? dailyReport = await this.context.DailyReports
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (dailyReport == null)
        {
            return null;
        }

        return new DailyReportDeleteViewModel
        {
            Id = dailyReport.Id,
            ChildFullName = $"{dailyReport.Child.FirstName} {dailyReport.Child.LastName}",
            ReportDate = dailyReport.ReportDate,
            Mood = dailyReport.Mood.ToString()
        };
    }

    public async Task DeleteAsync(int id, string userId, bool isAdmin, bool isTeacher)
    {
        bool canManage = await this.CanManageAsync(id, userId, isAdmin, isTeacher);

        if (!canManage)
        {
            throw new InvalidOperationException("Daily report not found.");
        }

        DailyReport? dailyReport = await this.context.DailyReports
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (dailyReport == null)
        {
            throw new InvalidOperationException("Daily report not found.");
        }

        dailyReport.IsDeleted = true;

        await this.context.SaveChangesAsync();
    }

    private async Task<bool> CanManageAsync(int dailyReportId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await this.context.DailyReports
                .AnyAsync(r => r.Id == dailyReportId && !r.IsDeleted);
        }

        if (!isTeacher)
        {
            return false;
        }

        return await this.context.DailyReports.AnyAsync(r =>
            r.Id == dailyReportId &&
            !r.IsDeleted &&
            r.CreatedByUserId == userId);
    }

    private async Task<bool> CanManageChildAsync(int childId, string userId, bool isAdmin, bool isTeacher)
    {
        if (isAdmin)
        {
            return await this.context.Children
                .AnyAsync(c => c.Id == childId && !c.IsDeleted);
        }

        if (!isTeacher)
        {
            return false;
        }

        int? teacherGroupId = await this.GetTeacherGroupIdAsync(userId);

        return teacherGroupId.HasValue &&
               await this.context.Children.AnyAsync(c =>
                   c.Id == childId &&
                   !c.IsDeleted &&
                   c.GroupId == teacherGroupId.Value);
    }

    private async Task<IEnumerable<SelectListItem>> GetChildrenSelectListAsync(string userId, bool isAdmin, bool isTeacher)
    {
        var query = this.context.Children
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            int? teacherGroupId = await this.GetTeacherGroupIdAsync(userId);

            if (!teacherGroupId.HasValue)
            {
                return new List<SelectListItem>();
            }

            query = query.Where(c => c.GroupId == teacherGroupId.Value);
        }
        else if (!isAdmin)
        {
            return new List<SelectListItem>();
        }

        return await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FirstName + " " + c.LastName
            })
            .ToListAsync();
    }

    private async Task<int?> GetTeacherGroupIdAsync(string userId)
    {
        return await this.context.TeacherProfiles
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .Select(t => (int?)t.GroupId)
            .FirstOrDefaultAsync();
    }
}