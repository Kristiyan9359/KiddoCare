using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Events;

public class EventDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public EventType Type { get; set; }

    public string? Location { get; set; }

    public string GroupName { get; set; } = "All groups";

    public bool IsPublic { get; set; }
}