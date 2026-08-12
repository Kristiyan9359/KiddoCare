using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.AbsenceRequests;

public class AbsenceRequestCreateViewModel
{
    [Display(Name = "Child")]
    [Required(ErrorMessage = "Please select a child.")]
    public int? ChildId { get; set; }

    [Display(Name = "Start date")]
    [Required(ErrorMessage = "Please select the first absence date.")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "End date")]
    [Required(ErrorMessage = "Please select the last absence date.")]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "Reason")]
    [Required(ErrorMessage = "Please select a reason.")]
    public AbsenceReason Reason { get; set; }

    [Display(Name = "Note")]
    [MaxLength(AbsenceRequestParentNoteMaxLength, ErrorMessage = "Note cannot be longer than {1} characters.")]
    public string? ParentNote { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}