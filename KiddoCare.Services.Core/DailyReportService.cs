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

    public async Task<DailyReportListViewModel> GetAllAsync(string userId, bool isAdmin, bool isTeacher, string? searchTerm, int page, int pageSize)
    {
        var query = this.context.DailyReports
            .Where(r => !r.IsDeleted && !r.Child.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            int? teacherGroupId = await this.GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new DailyReportListViewModel
                {
                    SearchTerm = searchTerm,
                    Page = 1,
                    PageSize = pageSize
                };
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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            var matchingMoods = Enum.GetValues<ChildMood>()
                .Where(m => m.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            query = query.Where(r =>
                (r.Child.FirstName + " " + r.Child.LastName).Contains(searchTerm) ||
                r.Child.Group.Name.Contains(searchTerm) ||
                matchingMoods.Contains(r.Mood));
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 15 or 20 ? pageSize : 15;

        var totalDailyReports = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalDailyReports / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var dailyReports = await query
            .OrderByDescending(r => r.ReportDate)
            .ThenBy(r => r.Child.FirstName)
            .ThenBy(r => r.Child.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new DailyReportIndexViewModel
            {
                Id = r.Id,
                ChildId = r.ChildId,
                ChildFullName = r.Child.FirstName + " " + r.Child.LastName,
                GroupName = r.Child.Group.Name,
                ReportDate = r.ReportDate,
                Mood = r.Mood,
                MealRating = r.MealRating,
                SleepRating = r.SleepRating,
                ActivityRating = r.ActivityRating,
                CanManage = isAdmin || (isTeacher && r.CreatedByUserId == userId)
            })
            .ToListAsync();

        return new DailyReportListViewModel
        {
            DailyReports = dailyReports,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalDailyReports = totalDailyReports
        };
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string term, string userId, bool isAdmin, bool isTeacher)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<string>();
        }

        term = term.Trim();

        var query = this.context.DailyReports
            .Where(r => !r.IsDeleted && !r.Child.IsDeleted)
            .AsQueryable();

        if (isTeacher && !isAdmin)
        {
            int? teacherGroupId = await this.GetTeacherGroupIdAsync(userId);

            if (teacherGroupId == null)
            {
                return new List<string>();
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

        var matchingMoods = Enum.GetValues<ChildMood>()
            .Where(m => m.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Where(r =>
                (r.Child.FirstName + " " + r.Child.LastName).Contains(term) ||
                r.Child.Group.Name.Contains(term) ||
                matchingMoods.Contains(r.Mood))
            .OrderByDescending(r => r.ReportDate)
            .Select(r => r.Child.FirstName + " " + r.Child.LastName)
            .Distinct()
            .Take(8)
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
                MealRating = r.MealRating,
                SleepRating = r.SleepRating,
                ActivityRating = r.ActivityRating,
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

        ValidateRatings(model.MealRating, model.SleepRating, model.ActivityRating);

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
            MealRating = model.MealRating,
            SleepRating = model.SleepRating,
            ActivityRating = model.ActivityRating,
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
                MealRating = r.MealRating,
                SleepRating = r.SleepRating,
                ActivityRating = r.ActivityRating,
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

        ValidateRatings(model.MealRating, model.SleepRating, model.ActivityRating);

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
        dailyReport.MealRating = model.MealRating;
        dailyReport.SleepRating = model.SleepRating;
        dailyReport.ActivityRating = model.ActivityRating;
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

    private static void ValidateRatings(int mealRating, int sleepRating, int activityRating)
    {
        if (!IsValidRating(mealRating) ||
            !IsValidRating(sleepRating) ||
            !IsValidRating(activityRating))
        {
            throw new InvalidOperationException("Daily report ratings must be between 1 and 5.");
        }
    }

    private static bool IsValidRating(int rating)
    {
        return rating is >= 1 and <= 5;
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
