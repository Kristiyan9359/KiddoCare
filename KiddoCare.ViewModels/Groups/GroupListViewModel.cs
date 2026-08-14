namespace KiddoCare.ViewModels.Groups;

public class GroupListViewModel
{
    public IEnumerable<GroupIndexViewModel> Groups { get; set; } = new List<GroupIndexViewModel>();

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalGroups { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalGroups / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}