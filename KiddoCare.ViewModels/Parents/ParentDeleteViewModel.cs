namespace KiddoCare.ViewModels.Parents;

public class ParentDeleteViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int ChildrenCount { get; set; }
}