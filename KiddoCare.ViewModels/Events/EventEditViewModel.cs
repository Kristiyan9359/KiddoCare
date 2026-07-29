using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Events;

public class EventEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(EventTitleMaxLength)]
    public string Title { get; set; } = null!;

    [MaxLength(EventDescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    [Required]
    public EventType Type { get; set; }

    [MaxLength(EventLocationMaxLength)]
    public string? Location { get; set; }

    public int? GroupId { get; set; }

    public bool IsPublic { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}