using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Events;

public class EventEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Event title")]
    [Required(ErrorMessage = "Please enter an event title.")]
    [MaxLength(EventTitleMaxLength, ErrorMessage = "Event title cannot be longer than {1} characters.")]
    public string Title { get; set; } = null!;

    [Display(Name = "Description")]
    [MaxLength(EventDescriptionMaxLength, ErrorMessage = "Description cannot be longer than {1} characters.")]
    public string? Description { get; set; }

    [Display(Name = "Starts On")]
    [Required(ErrorMessage = "Please select when the event starts.")]
    public DateTime StartDateTime { get; set; }

    [Display(Name = "Ends On")]
    public DateTime? EndDateTime { get; set; }

    [Display(Name = "Event type")]
    [Required(ErrorMessage = "Please select an event type.")]
    public EventType Type { get; set; }

    [Display(Name = "Location")]
    [MaxLength(EventLocationMaxLength, ErrorMessage = "Location cannot be longer than {1} characters.")]
    public string? Location { get; set; }

    [Display(Name = "Group")]
    public int? GroupId { get; set; }

    [Display(Name = "Visible to parents")]
    public bool IsPublic { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}
