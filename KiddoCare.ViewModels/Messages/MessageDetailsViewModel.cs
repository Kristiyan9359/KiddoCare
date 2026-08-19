namespace KiddoCare.ViewModels.Messages;

public class MessageDetailsViewModel
{
    public int ConversationId { get; set; }

    public string ChildFullName { get; set; } = null!;

    public string OtherParticipantName { get; set; } = null!;

    public IEnumerable<MessageConversationViewModel> Conversations { get; set; } = new List<MessageConversationViewModel>();

    public IEnumerable<MessageItemViewModel> Messages { get; set; } = new List<MessageItemViewModel>();
}