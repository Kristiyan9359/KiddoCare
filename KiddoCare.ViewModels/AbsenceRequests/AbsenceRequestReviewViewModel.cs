using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.AbsenceRequests;

public class AbsenceRequestReviewViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AbsenceReason Reason { get; set; }

    public string? ParentNote { get; set; }

    [Display(Name = "Review Note")]
    [MaxLength(AbsenceRequestReviewNoteMaxLength)]
    public string? ReviewNote { get; set; }

    public string? ReturnUrl { get; set; }
}
