using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class MedicalRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [MaxLength(MedicalRecordAllergiesMaxLength)]
    public string? Allergies { get; set; }

    [MaxLength(MedicalRecordChronicConditionsMaxLength)]
    public string? ChronicConditions { get; set; }

    [MaxLength(MedicalRecordDoctorNameMaxLength)]
    public string? DoctorName { get; set; }

    [MaxLength(MedicalRecordDoctorPhoneMaxLength)]
    public string? DoctorPhone { get; set; }

    [MaxLength(MedicalRecordEmergencyContactNameMaxLength)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(MedicalRecordEmergencyContactPhoneMaxLength)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(MedicalRecordNotesMaxLength)]
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
}