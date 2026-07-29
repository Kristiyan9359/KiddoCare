using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;
using KiddoCare.Common.ValidationAttributes;

namespace KiddoCare.ViewModels.Children;

public class ChildCreateViewModel
{
    [Required]
    [MaxLength(ChildFirstNameMaxLength)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(ChildLastNameMaxLength)]
    public string LastName { get; set; } = null!;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    [ChildBirthDate]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-6);

    [MaxLength(ChildAllergiesMaxLength)]
    public string? Allergies { get; set; }

    [MaxLength(ChildAdditionalNotesMaxLength)]
    public string? AdditionalNotes { get; set; }

    [Required]
    public int GroupId { get; set; }

    [MaxLength(ChildPhotoUrlMaxLength)]
    public string? PhotoUrl { get; set; }

    public int? ParentId { get; set; }

    public IEnumerable<SelectListItem> Parents { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}