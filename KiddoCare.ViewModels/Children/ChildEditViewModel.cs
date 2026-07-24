using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Children;

public class ChildEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(ChildFirstNameMaxLength)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(ChildLastNameMaxLength)]
    public string LastName { get; set; } = null!;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(ChildAllergiesMaxLength)]
    public string? Allergies { get; set; }

    [MaxLength(ChildAdditionalNotesMaxLength)]
    public string? AdditionalNotes { get; set; }

    [Required]
    public int GroupId { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}