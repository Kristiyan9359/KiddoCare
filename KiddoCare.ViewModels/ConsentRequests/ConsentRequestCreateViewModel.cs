using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.ConsentRequests;

public class ConsentRequestCreateViewModel
{
    [Display(Name = "Child")]
    [Required(ErrorMessage = "Please select a child.")]
    public int? ChildId { get; set; }

    [Display(Name = "Consent title")]
    [Required(ErrorMessage = "Please enter a consent title.")]
    [MaxLength(ConsentRequestTitleMaxLength, ErrorMessage = "Consent title cannot be longer than {1} characters.")]
    public string Title { get; set; } = null!;

    [Display(Name = "Description")]
    [MaxLength(ConsentRequestDescriptionMaxLength, ErrorMessage = "Description cannot be longer than {1} characters.")]
    public string? Description { get; set; }

    [Display(Name = "Consent type")]
    [Required(ErrorMessage = "Please select a consent type.")]
    public ConsentRequestType Type { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}