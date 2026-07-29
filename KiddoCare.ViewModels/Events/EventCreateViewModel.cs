using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Events;

public class EventCreateViewModel
{
    [Required]
    [MaxLength(EventTitleMaxLength)]
    public string Title { get; set; } = null!;

    [MaxLength(EventDescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; } = DateTime.Now.AddDays(1);

    public DateTime? EndDateTime { get; set; }

    [Required]
    public EventType Type { get; set; } = EventType.General;

    [MaxLength(EventLocationMaxLength)]
    public string? Location { get; set; }

    public int? GroupId { get; set; }

    public bool IsPublic { get; set; } = true;

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}