namespace KiddoCare.ViewModels.Messages;

public class MessageConversationViewModel
{
    public int Id { get; set; }

    public string? ChildFullName { get; set; }

    public string ConversationType { get; set; } = null!;

    public string OtherParticipantName { get; set; } = null!;

    public string LastMessagePreview { get; set; } = null!;

    public DateTime? LastMessageOn { get; set; }

    public int UnreadMessagesCount { get; set; }
}
