namespace KiddoCare.ViewModels.DailyReports;

public class DailyReportListViewModel
{
    public IEnumerable<DailyReportIndexViewModel> DailyReports { get; set; } = new List<DailyReportIndexViewModel>();

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalDailyReports { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalDailyReports / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
