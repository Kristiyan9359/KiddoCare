namespace KiddoCare.ViewModels.Parents;

public class ParentIndexViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int ChildrenCount { get; set; }
}