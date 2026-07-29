namespace KiddoCare.ViewModels.Groups;

public class GroupDetailsViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public IEnumerable<GroupChildViewModel> Children { get; set; } = new List<GroupChildViewModel>();
}