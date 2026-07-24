namespace KiddoCare.ViewModels.Children;

public class ChildIndexViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public string GroupName { get; set; } = null!;
}