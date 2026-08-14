namespace KiddoCare.ViewModels.Announcements;

public class AnnouncementListViewModel
{
    public IEnumerable<AnnouncementIndexViewModel> Announcements { get; set; } = new List<AnnouncementIndexViewModel>();

    public string? SearchTerm { get; set; }

    public string? ReturnUrl { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalAnnouncements { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalAnnouncements / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
