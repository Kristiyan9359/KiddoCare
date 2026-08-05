using KiddoCare.Data.Models.Enums;

namespace KiddoCare.ViewModels.Children;

public class ChildDetailsViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string GroupName { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public string? ParentName { get; set; }

    public string? ParentEmail { get; set; }

    public string? ParentPhoneNumber { get; set; }

    public bool HasMedicalRecord { get; set; }

    public string? MedicalAllergies { get; set; }

    public string? MedicalChronicConditions { get; set; }

    public string? MedicalEmergencyContactName { get; set; }

    public string? MedicalEmergencyContactPhone { get; set; }

    public IEnumerable<ChildDetailsAbsenceRequestViewModel> RecentAbsenceRequests { get; set; } = new List<ChildDetailsAbsenceRequestViewModel>();
}