namespace KiddoCare.Data.Models;

using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;
using KiddoCare.Data.Models.Enums;

public class DailyReport
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime ReportDate { get; set; }

    [Required]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [Required]
    public ChildMood Mood { get; set; }

    [MaxLength(DailyReportMealsMaxLength)]
    public string? Meals { get; set; }

    [MaxLength(DailyReportSleepMaxLength)]
    public string? Sleep { get; set; }

    [MaxLength(DailyReportActivitiesMaxLength)]
    public string? Activities { get; set; }

    [MaxLength(DailyReportTeacherNoteMaxLength)]
    public string? TeacherNote { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
}