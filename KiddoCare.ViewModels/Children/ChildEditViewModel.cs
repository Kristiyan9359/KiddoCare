using KiddoCare.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;
using KiddoCare.Common.ValidationAttributes;

namespace KiddoCare.ViewModels.Children;

public class ChildEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Please enter the child's first name.")]
    [MaxLength(ChildFirstNameMaxLength, ErrorMessage = "First name cannot be longer than {1} characters.")]
    public string FirstName { get; set; } = null!;

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Please enter the child's last name.")]
    [MaxLength(ChildLastNameMaxLength, ErrorMessage = "Last name cannot be longer than {1} characters.")]
    public string LastName { get; set; } = null!;

    [Display(Name = "Gender")]
    [Required(ErrorMessage = "Please select the child's gender.")]
    public Gender Gender { get; set; }

    [Display(Name = "Date of birth")]
    [Required(ErrorMessage = "Please select the child's date of birth.")]
    [ChildBirthDate]
    public DateTime DateOfBirth { get; set; }

    [Display(Name = "Group")]
    [Required(ErrorMessage = "Please select a group.")]
    public int GroupId { get; set; }

    [MaxLength(ChildPhotoUrlMaxLength)]
    public string? PhotoUrl { get; set; }

    [Display(Name = "Profile photo")]
    public IFormFile? Photo { get; set; }

    [Display(Name = "Remove current photo")]
    public bool RemovePhoto { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();

    [Display(Name = "Parent")]
    public int? ParentId { get; set; }

    public IEnumerable<SelectListItem> Parents { get; set; } = new List<SelectListItem>();
}
