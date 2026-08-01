using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Children;

public class ChildIndexViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string GroupName { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public bool HasMedicalRecord { get; set; }

    public bool HasAllergies { get; set; }
}
