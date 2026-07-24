using KiddoCare.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class AttendanceRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Child))]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    [MaxLength(AttendanceNoteMaxLength)]
    public string? Note { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}