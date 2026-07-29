using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Events;

public class EventIndexViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public DateTime StartDateTime { get; set; }

    public EventType Type { get; set; }

    public string? Location { get; set; }

    public string GroupName { get; set; } = "All groups";

    public bool CanManage { get; set; }
}