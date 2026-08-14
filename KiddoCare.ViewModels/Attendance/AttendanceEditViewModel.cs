using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceEditViewModel
{
    public int Id { get; set; }

    [ValidateNever]
    public DateTime Date { get; set; }

    [ValidateNever]
    public string ChildName { get; set; } = null!;

    [ValidateNever]
    public string GroupName { get; set; } = null!;

    [Required]
    public AttendanceStatus Status { get; set; }

    [MaxLength(AttendanceNoteMaxLength)]
    public string? Note { get; set; }

    public string? ReturnUrl { get; set; }
}