namespace KiddoCare.ViewModels.Children;

public class ChildDeleteViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }
}