namespace KiddoCare.ViewModels.DailyReports;

using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

public class DailyReportCreateViewModel
{
    [Display(Name = "Child")]
    [Required(ErrorMessage = "Please select a child.")]
    public int? ChildId { get; set; }

    [Display(Name = "Report Date")]
    [Required(ErrorMessage = "Please select a report date.")]
    [DataType(DataType.Date)]
    public DateTime ReportDate { get; set; } = DateTime.Today;

    [Display(Name = "Mood")]
    [Required(ErrorMessage = "Please select the child's mood.")]
    [Range(1, 5, ErrorMessage = "Please select the child's mood.")]
    public ChildMood? Mood { get; set; }

    [Display(Name = "Meals")]
    [Required(ErrorMessage = "Please rate the child's meals.")]
    [Range(1, 5, ErrorMessage = "Please rate the child's meals.")]
    public int? MealRating { get; set; }

    [Display(Name = "Sleep")]
    [Required(ErrorMessage = "Please rate the child's sleep.")]
    [Range(1, 5, ErrorMessage = "Please rate the child's sleep.")]
    public int? SleepRating { get; set; }

    [Display(Name = "Activities")]
    [Required(ErrorMessage = "Please rate the child's activities.")]
    [Range(1, 5, ErrorMessage = "Please rate the child's activities.")]
    public int? ActivityRating { get; set; }

    [Display(Name = "Teacher Note")]
    [MaxLength(DailyReportTeacherNoteMaxLength, ErrorMessage = "Teacher note cannot be longer than {1} characters.")]
    public string? TeacherNote { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } =
        new List<SelectListItem>();
}
