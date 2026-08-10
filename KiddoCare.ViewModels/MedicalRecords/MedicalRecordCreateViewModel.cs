using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.MedicalRecords;

public class MedicalRecordCreateViewModel
{
    [Required]
    [Display(Name = "Child")]
    public int? ChildId { get; set; }

    [MaxLength(MedicalRecordAllergiesMaxLength)]
    public string? Allergies { get; set; }

    [Display(Name = "Chronic Conditions")]
    [MaxLength(MedicalRecordChronicConditionsMaxLength)]
    public string? ChronicConditions { get; set; }

    [Display(Name = "Doctor Name")]
    [MaxLength(MedicalRecordDoctorNameMaxLength)]
    public string? DoctorName { get; set; }

    [Display(Name = "Doctor Phone")]
    [MaxLength(MedicalRecordDoctorPhoneMaxLength)]
    public string? DoctorPhone { get; set; }

    [Display(Name = "Emergency Contact Name")]
    [MaxLength(MedicalRecordEmergencyContactNameMaxLength)]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "Emergency Contact Phone")]
    [MaxLength(MedicalRecordEmergencyContactPhoneMaxLength)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(MedicalRecordNotesMaxLength)]
    public string? Notes { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}
