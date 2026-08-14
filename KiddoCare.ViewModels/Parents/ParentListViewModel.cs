namespace KiddoCare.ViewModels.Parents;

public class ParentListViewModel
{
    public IEnumerable<ParentIndexViewModel> Parents { get; set; } = new List<ParentIndexViewModel>();

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalParents { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalParents / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}