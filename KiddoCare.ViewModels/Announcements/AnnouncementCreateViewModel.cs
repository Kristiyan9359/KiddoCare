using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Announcements;

public class AnnouncementCreateViewModel
{
    [Required]
    [MaxLength(AnnouncementTitleMaxLength)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(AnnouncementContentMaxLength)]
    public string Content { get; set; } = null!;

    public int? GroupId { get; set; }

    public bool IsPublic { get; set; } = true;

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}