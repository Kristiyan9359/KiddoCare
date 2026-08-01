namespace KiddoCare.ViewModels.MedicalRecords;

public class MedicalRecordDetailsViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string? Allergies { get; set; }

    public string? ChronicConditions { get; set; }

    public string? DoctorName { get; set; }

    public string? DoctorPhone { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? Notes { get; set; }

    public bool CanManage { get; set; }
}