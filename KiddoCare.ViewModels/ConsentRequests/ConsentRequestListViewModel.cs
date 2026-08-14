namespace KiddoCare.ViewModels.ConsentRequests;

public class ConsentRequestListViewModel
{
    public IEnumerable<ConsentRequestIndexViewModel> ConsentRequests { get; set; } = new List<ConsentRequestIndexViewModel>();

    public string? SearchTerm { get; set; }

    public string? StatusFilter { get; set; }

    public string? ReturnUrl { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalConsentRequests { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalConsentRequests / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
