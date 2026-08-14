namespace KiddoCare.ViewModels.Events;

public class EventListViewModel
{
    public IEnumerable<EventIndexViewModel> Events { get; set; } = new List<EventIndexViewModel>();

    public string? SearchTerm { get; set; }

    public string? ReturnUrl { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalEvents { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalEvents / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
