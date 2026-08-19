namespace KiddoCare.Data.Models;

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Conversation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Child))]
    public int ChildId { get; set; }

    public Child Child { get; set; } = null!;

    [Required]
    public string ParentUserId { get; set; } = null!;

    [ForeignKey(nameof(ParentUserId))]
    public IdentityUser ParentUser { get; set; } = null!;

    [Required]
    public string TeacherUserId { get; set; } = null!;

    [ForeignKey(nameof(TeacherUserId))]
    public IdentityUser TeacherUser { get; set; } = null!;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageOn { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new HashSet<ChatMessage>();
}