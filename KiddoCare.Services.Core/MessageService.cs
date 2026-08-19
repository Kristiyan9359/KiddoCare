namespace KiddoCare.Services.Core;

using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Messages;
using Microsoft.EntityFrameworkCore;

public class MessageService : IMessageService
{
    private const string FullNameClaimType = "FullName";

    private readonly ApplicationDbContext context;

    public MessageService(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<MessageConversationViewModel>> GetConversationsAsync(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var conversations = await GetAccessibleConversations(userId, isAdmin, isTeacher, isParent)
            .Select(c => new
            {
                c.Id,
                c.ParentUserId,
                c.TeacherUserId,
                ChildFullName = c.Child.FirstName + " " + c.Child.LastName,
                c.CreatedOn,
                LastMessageOn = c.Messages.Where(m => !m.IsDeleted).OrderByDescending(m => m.SentOn).Select(m => (DateTime?)m.SentOn).FirstOrDefault(),
                LastMessagePreview = c.Messages.Where(m => !m.IsDeleted).OrderByDescending(m => m.SentOn).Select(m => m.Content).FirstOrDefault(),
                UnreadMessagesCount = c.Messages.Count(m => !m.IsDeleted && m.SenderUserId != userId && m.ReadOn == null)
            })
            .OrderByDescending(c => c.LastMessageOn ?? c.CreatedOn)
            .ToListAsync();

        var participantIds = conversations
            .SelectMany(c => new[] { c.ParentUserId, c.TeacherUserId })
            .Distinct()
            .ToList();

        var displayNames = await GetDisplayNamesByUserIdsAsync(participantIds);

        return conversations.Select(c => new MessageConversationViewModel
        {
            Id = c.Id,
            ChildFullName = c.ChildFullName,
            OtherParticipantName = GetOtherParticipantName(c.ParentUserId, c.TeacherUserId, userId, isAdmin, displayNames),
            LastMessagePreview = CreatePreview(c.LastMessagePreview),
            LastMessageOn = c.LastMessageOn,
            UnreadMessagesCount = c.UnreadMessagesCount
        });
    }

    public async Task<MessageDetailsViewModel?> GetDetailsAsync(int conversationId, string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        bool canAccess = await CanAccessConversationAsync(conversationId, userId, isAdmin, isTeacher, isParent);

        if (!canAccess)
        {
            return null;
        }

        var conversation = await context.Conversations
            .Where(c => c.Id == conversationId && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.ParentUserId,
                c.TeacherUserId,
                ChildFullName = c.Child.FirstName + " " + c.Child.LastName
            })
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            return null;
        }

        var unreadMessages = await context.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted && m.SenderUserId != userId && m.ReadOn == null)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.ReadOn = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        var messages = await context.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderBy(m => m.SentOn)
            .Select(m => new
            {
                m.Id,
                m.SenderUserId,
                m.Content,
                m.SentOn
            })
            .ToListAsync();

        var senderIds = messages.Select(m => m.SenderUserId).Distinct().ToList();
        var displayNames = await GetDisplayNamesByUserIdsAsync(senderIds);

        return new MessageDetailsViewModel
        {
            ConversationId = conversation.Id,
            ChildFullName = conversation.ChildFullName,
            OtherParticipantName = GetOtherParticipantName(conversation.ParentUserId, conversation.TeacherUserId, userId, isAdmin, displayNames),
            Conversations = await GetConversationsAsync(userId, isAdmin, isTeacher, isParent),
            Messages = messages.Select(m => new MessageItemViewModel
            {
                Id = m.Id,
                SenderName = displayNames.GetValueOrDefault(m.SenderUserId, "Unknown user"),
                Content = m.Content,
                SentOn = m.SentOn,
                IsMine = m.SenderUserId == userId
            })
        };
    }

    public async Task<bool> CanAccessConversationAsync(int conversationId, string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var query = context.Conversations.Where(c => c.Id == conversationId && !c.IsDeleted);

        if (isAdmin)
        {
            return await query.AnyAsync();
        }

        if (isTeacher)
        {
            return await query.AnyAsync(c => c.TeacherUserId == userId);
        }

        if (isParent)
        {
            return await query.AnyAsync(c => c.ParentUserId == userId);
        }

        return false;
    }

    public async Task SendMessageAsync(SendMessageInputModel model, string senderUserId, bool isAdmin, bool isTeacher, bool isParent)
    {
        bool canAccess = await CanAccessConversationAsync(model.ConversationId, senderUserId, isAdmin, isTeacher, isParent);

        if (!canAccess)
        {
            throw new UnauthorizedAccessException();
        }

        var conversation = await context.Conversations.FirstAsync(c => c.Id == model.ConversationId && !c.IsDeleted);
        var sentOn = DateTime.UtcNow;

        await context.ChatMessages.AddAsync(new ChatMessage
        {
            ConversationId = model.ConversationId,
            SenderUserId = senderUserId,
            Content = model.Content.Trim(),
            SentOn = sentOn
        });

        conversation.LastMessageOn = sentOn;

        await context.SaveChangesAsync();
    }

    private IQueryable<Conversation> GetAccessibleConversations(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var query = context.Conversations.Where(c => !c.IsDeleted);

        if (isAdmin)
        {
            return query;
        }

        if (isTeacher)
        {
            return query.Where(c => c.TeacherUserId == userId);
        }

        if (isParent)
        {
            return query.Where(c => c.ParentUserId == userId);
        }

        return query.Where(c => false);
    }

    private async Task<Dictionary<string, string>> GetDisplayNamesByUserIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        var names = new Dictionary<string, string>();

        var parentNames = await context.ParentProfiles
            .Where(p => ids.Contains(p.UserId) && !p.IsDeleted)
            .Select(p => new { p.UserId, p.FullName })
            .ToListAsync();

        foreach (var parent in parentNames)
        {
            names[parent.UserId] = parent.FullName;
        }

        var teacherNames = await context.TeacherProfiles
            .Where(t => ids.Contains(t.UserId) && !t.IsDeleted)
            .Select(t => new { t.UserId, t.FullName })
            .ToListAsync();

        foreach (var teacher in teacherNames)
        {
            names[teacher.UserId] = teacher.FullName;
        }

        var claimNames = await context.UserClaims
            .Where(c => ids.Contains(c.UserId) && c.ClaimType == FullNameClaimType && c.ClaimValue != null)
            .Select(c => new { c.UserId, c.ClaimValue })
            .ToListAsync();

        foreach (var claim in claimNames.Where(c => !names.ContainsKey(c.UserId)))
        {
            names[claim.UserId] = claim.ClaimValue!;
        }

        var users = await context.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.UserName })
            .ToListAsync();

        foreach (var user in users.Where(u => !names.ContainsKey(u.Id)))
        {
            names[user.Id] = user.Email ?? user.UserName ?? "Unknown user";
        }

        return names;
    }

    private static string GetOtherParticipantName(string parentUserId, string teacherUserId, string currentUserId, bool isAdmin, Dictionary<string, string> displayNames)
    {
        if (isAdmin)
        {
            return $"{displayNames.GetValueOrDefault(parentUserId, "Parent")} / {displayNames.GetValueOrDefault(teacherUserId, "Teacher")}";
        }

        string otherUserId = currentUserId == parentUserId ? teacherUserId : parentUserId;

        return displayNames.GetValueOrDefault(otherUserId, "Unknown user");
    }

    private static string CreatePreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "No messages yet";
        }

        return content.Length <= 80 ? content : content[..80] + "...";
    }
}