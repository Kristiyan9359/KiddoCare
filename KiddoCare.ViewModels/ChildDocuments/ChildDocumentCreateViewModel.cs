using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.ChildDocuments;

public class ChildDocumentCreateViewModel
{
    [Required]
    [Display(Name = "Child")]
    public int? ChildId { get; set; }

    [Required]
    public ChildDocumentType Type { get; set; }

    [Required]
    [MaxLength(ChildDocumentTitleMaxLength)]
    public string Title { get; set; } = null!;

    [Required]
    [Display(Name = "Document File")]
    public IFormFile File { get; set; } = null!;

    public string? FileUrl { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}
