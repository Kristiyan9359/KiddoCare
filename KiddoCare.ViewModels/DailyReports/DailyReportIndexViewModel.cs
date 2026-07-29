namespace KiddoCare.ViewModels.DailyReports;

using KiddoCare.Data.Models.Enums;

public class DailyReportIndexViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public ChildMood Mood { get; set; }

    public bool CanManage { get; set; }
}