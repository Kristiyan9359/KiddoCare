namespace KiddoCare.ViewModels.ChildDocuments;

public class ChildDocumentListViewModel
{
    public IEnumerable<ChildDocumentIndexViewModel> Documents { get; set; } = new List<ChildDocumentIndexViewModel>();

    public string? SearchTerm { get; set; }

    public string? StatusFilter { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalDocuments { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalDocuments / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
