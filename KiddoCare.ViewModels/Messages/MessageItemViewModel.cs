namespace KiddoCare.ViewModels.Messages;

public class MessageItemViewModel
{
    public int Id { get; set; }

    public string SenderName { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime SentOn { get; set; }

    public bool IsMine { get; set; }
}