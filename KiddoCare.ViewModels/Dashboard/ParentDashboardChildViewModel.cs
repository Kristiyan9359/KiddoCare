using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Dashboard;

public class ParentDashboardChildViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string? PhotoUrl { get; set; }

    public string GroupName { get; set; } = null!;

    public int? LastDailyReportId { get; set; }

    public DateTime? LastDailyReportDate { get; set; }

    public ChildMood? LastDailyReportMood { get; set; }

    public int? LastMealRating { get; set; }

    public int? LastSleepRating { get; set; }

    public int? LastActivityRating { get; set; }
}
