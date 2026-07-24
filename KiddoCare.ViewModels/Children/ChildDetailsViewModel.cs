using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Children;

public class ChildDetailsViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string GroupName { get; set; } = null!;

    public string? Allergies { get; set; }

    public string? AdditionalNotes { get; set; }

    public string? PhotoUrl { get; set; }
}