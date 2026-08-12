using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Teachers;

public class TeacherCreateViewModel
{
    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Please enter the teacher's email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Display(Name = "Full name")]
    [Required(ErrorMessage = "Please enter the teacher's full name.")]
    [MaxLength(TeacherFullNameMaxLength, ErrorMessage = "Full name cannot be longer than {1} characters.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Phone number")]
    [MaxLength(TeacherPhoneNumberMaxLength, ErrorMessage = "Phone number cannot be longer than {1} characters.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Assigned group")]
    [Required(ErrorMessage = "Please select a group.")]
    public int GroupId { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}