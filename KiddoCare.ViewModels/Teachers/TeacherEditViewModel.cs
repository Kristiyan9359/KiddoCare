using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Teachers;

public class TeacherEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(TeacherFullNameMaxLength)]
    public string FullName { get; set; } = null!;

    [MaxLength(TeacherPhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

    [Required]
    public int GroupId { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}