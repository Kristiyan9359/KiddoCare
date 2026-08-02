using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;
using KiddoCare.Data.Models.Enums;

namespace KiddoCare.Data.Models;

public class Child
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(ChildFirstNameMaxLength)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(ChildLastNameMaxLength)]
    public string LastName { get; set; } = null!;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [ForeignKey(nameof(Group))]
    public int GroupId { get; set; }

    [MaxLength(ChildPhotoUrlMaxLength)]
    public string? PhotoUrl { get; set; }
    public KindergartenGroup Group { get; set; } = null!;

    [ForeignKey(nameof(Parent))]
    public int? ParentId { get; set; }

    public ParentProfile? Parent { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new HashSet<MedicalRecord>();

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new HashSet<AttendanceRecord>();

    public ICollection<DailyReport> DailyReports { get; set; } = new HashSet<DailyReport>();
}