namespace KiddoCare.ViewModels.AbsenceRequests;

public class AbsenceRequestListViewModel
{
    public IEnumerable<AbsenceRequestIndexViewModel> AbsenceRequests { get; set; } = new List<AbsenceRequestIndexViewModel>();

    public string? SearchTerm { get; set; }

    public string? StatusFilter { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalAbsenceRequests { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalAbsenceRequests / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
