using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.ViewModels.Announcements;

public class AnnouncementEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Announcement title")]
    [Required(ErrorMessage = "Please enter an announcement title.")]
    [MaxLength(AnnouncementTitleMaxLength, ErrorMessage = "Announcement title cannot be longer than {1} characters.")]
    public string Title { get; set; } = null!;

    [Display(Name = "Message")]
    [Required(ErrorMessage = "Please enter the announcement message.")]
    [MaxLength(AnnouncementContentMaxLength, ErrorMessage = "Message cannot be longer than {1} characters.")]
    public string Content { get; set; } = null!;

    [Display(Name = "Group")]
    public int? GroupId { get; set; }

    [Display(Name = "Visible to parents")]
    public bool IsPublic { get; set; }

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
}