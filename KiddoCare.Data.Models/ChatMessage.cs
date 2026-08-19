namespace KiddoCare.Data.Models;

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Conversation))]
    public int ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    [Required]
    public string SenderUserId { get; set; } = null!;

    [ForeignKey(nameof(SenderUserId))]
    public IdentityUser SenderUser { get; set; } = null!;

    [Required]
    [MaxLength(ChatMessageContentMaxLength)]
    public string Content { get; set; } = null!;

    public DateTime SentOn { get; set; } = DateTime.UtcNow;

    public DateTime? ReadOn { get; set; }

    public bool IsDeleted { get; set; }
}