using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.ConsentRequests;

public class ConsentRequestCreateViewModel
{
    [Required]
    [Display(Name = "Child")]
    public int? ChildId { get; set; }

    [Required]
    [MaxLength(ConsentRequestTitleMaxLength)]
    public string Title { get; set; } = null!;

    [MaxLength(ConsentRequestDescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    public ConsentRequestType Type { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}