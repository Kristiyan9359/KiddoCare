namespace KiddoCare.ViewModels.DailyReports;

using KiddoCare.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

public class DailyReportEditViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Report Date")]
    public DateTime ReportDate { get; set; }

    [Range(1, 5, ErrorMessage = "Please select the child's mood.")]
    public ChildMood Mood { get; set; }

    [MaxLength(DailyReportMealsMaxLength)]
    public string? Meals { get; set; }

    [MaxLength(DailyReportSleepMaxLength)]
    public string? Sleep { get; set; }

    [MaxLength(DailyReportActivitiesMaxLength)]
    public string? Activities { get; set; }

    [MaxLength(DailyReportTeacherNoteMaxLength)]
    [Display(Name = "Teacher Note")]
    public string? TeacherNote { get; set; }
}