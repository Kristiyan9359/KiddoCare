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

    public DateTime? LastDailyReportDate { get; set; }

    public ChildMood? LastDailyReportMood { get; set; }
}