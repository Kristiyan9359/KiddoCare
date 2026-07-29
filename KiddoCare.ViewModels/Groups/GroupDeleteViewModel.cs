namespace KiddoCare.ViewModels.Groups;

public class GroupDeleteViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int ChildrenCount { get; set; }
}