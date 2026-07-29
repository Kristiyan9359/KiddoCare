namespace KiddoCare.ViewModels.DailyReports;

public class DailyReportDeleteViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public string Mood { get; set; } = null!;
}