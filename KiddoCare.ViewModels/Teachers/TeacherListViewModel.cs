namespace KiddoCare.ViewModels.Teachers;

public class TeacherListViewModel
{
    public IEnumerable<TeacherIndexViewModel> Teachers { get; set; } = new List<TeacherIndexViewModel>();

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalTeachers { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalTeachers / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}