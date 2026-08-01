using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Dashboard;

public class TeacherDashboardDailyReportViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public ChildMood Mood { get; set; }
}