namespace KiddoCare.ViewModels.MedicalRecords;

public class MedicalRecordDeleteViewModel
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string? ReturnUrl { get; set; }
}