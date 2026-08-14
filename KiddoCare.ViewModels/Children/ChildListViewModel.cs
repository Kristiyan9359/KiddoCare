namespace KiddoCare.ViewModels.Children;

public class ChildListViewModel
{
    public IEnumerable<ChildIndexViewModel> Children { get; set; } = new List<ChildIndexViewModel>();

    public string? SearchTerm { get; set; }

    public string? MedicalRecordsFilter { get; set; }

    public string? AllergiesFilter { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalChildren { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalChildren / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}