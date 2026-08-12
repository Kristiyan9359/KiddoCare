using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.ChildDocuments;

public class ChildDocumentCreateViewModel
{
    [Display(Name = "Child")]
    [Required(ErrorMessage = "Please select a child.")]
    public int? ChildId { get; set; }

    [Display(Name = "Document type")]
    [Required(ErrorMessage = "Please select a document type.")]
    public ChildDocumentType Type { get; set; }

    [Display(Name = "Document title")]
    [Required(ErrorMessage = "Please enter a document title.")]
    [MaxLength(ChildDocumentTitleMaxLength, ErrorMessage = "Document title cannot be longer than {1} characters.")]
    public string Title { get; set; } = null!;

    [Display(Name = "Document file")]
    [Required(ErrorMessage = "Please upload a document file.")]
    public IFormFile File { get; set; } = null!;

    public string? FileUrl { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}