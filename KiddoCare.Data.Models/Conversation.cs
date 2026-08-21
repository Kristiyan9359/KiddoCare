namespace KiddoCare.Data.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Identity;

public class Conversation
{
    [Key]
    public int Id { get; set; }

    public ConversationType Type { get; set; } = ConversationType.ParentTeacher;

    [ForeignKey(nameof(Child))]
    public int? ChildId { get; set; }

    public Child? Child { get; set; }

    public string? ParentUserId { get; set; }

    [ForeignKey(nameof(ParentUserId))]
    public IdentityUser? ParentUser { get; set; }

    public string? TeacherUserId { get; set; }

    [ForeignKey(nameof(TeacherUserId))]
    public IdentityUser? TeacherUser { get; set; }

    public string? AdminUserId { get; set; }

    [ForeignKey(nameof(AdminUserId))]
    public IdentityUser? AdminUser { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageOn { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new HashSet<ChatMessage>();

    public ICollection<ConversationDeletion> ConversationDeletions { get; set; } = new HashSet<ConversationDeletion>();
}
