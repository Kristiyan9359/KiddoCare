using System.ComponentModel.DataAnnotations;
using KiddoCare.Data.Models.Enums;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.ConsentRequests;

public class ConsentRequestRespondViewModel
{
    public int Id { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public ConsentRequestType Type { get; set; }

    public string? Description { get; set; }

    [Display(Name = "Your Response")]
    [Range(1, 2, ErrorMessage = "Please select your response.")]
    public RequestStatus Status { get; set; }

    [Display(Name = "Parent Note")]
    [MaxLength(ConsentRequestParentNoteMaxLength, ErrorMessage = "Parent note cannot be longer than {1} characters.")]
    public string? ParentNote { get; set; }

    public string? ReturnUrl { get; set; }
}
