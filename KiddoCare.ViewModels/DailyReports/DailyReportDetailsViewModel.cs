namespace KiddoCare.ViewModels.DailyReports;

using KiddoCare.Data.Models.Enums;

public class DailyReportDetailsViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public ChildMood Mood { get; set; }

    public string? Meals { get; set; }

    public string? Sleep { get; set; }

    public string? Activities { get; set; }

    public string? TeacherNote { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool CanManage { get; set; }
}