namespace KiddoCare.ViewModels.Messages;

using System.ComponentModel.DataAnnotations;
using static KiddoCare.Common.ValidationConstants;

public class SendMessageInputModel
{
    public int ConversationId { get; set; }

    [Required(ErrorMessage = "Please enter a message.")]
    [StringLength(ChatMessageContentMaxLength, ErrorMessage = "Message cannot be longer than {1} characters.")]
    public string Content { get; set; } = null!;
}