namespace KiddoCare.ViewModels.Parents;

public class ParentDetailsViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public IEnumerable<string> ChildrenNames { get; set; } = new List<string>();
}