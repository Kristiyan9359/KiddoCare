namespace KiddoCare.ViewModels.Messages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using static KiddoCare.Common.ValidationConstants;

public class MessageCreateViewModel
{
    [Required(ErrorMessage = "Please select a recipient.")]
    public string RecipientUserId { get; set; } = null!;

    public int? ChildId { get; set; }

    [Required(ErrorMessage = "Please enter a message.")]
    [StringLength(ChatMessageContentMaxLength, ErrorMessage = "Message cannot be longer than {1} characters.")]
    public string Content { get; set; } = null!;

    public IEnumerable<SelectListItem> Recipients { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Children { get; set; } = new List<SelectListItem>();
}
