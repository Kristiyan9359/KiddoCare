namespace KiddoCare.ViewModels.DailyReports;

using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

public class DailyReportCreateViewModel
{
    [Required]
    [Display(Name = "Child")]
    public int? ChildId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Report Date")]
    public DateTime ReportDate { get; set; } = DateTime.Today;

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

    public IEnumerable<SelectListItem> Children { get; set; } =
        new List<SelectListItem>();
}