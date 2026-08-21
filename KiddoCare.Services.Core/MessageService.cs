namespace KiddoCare.Services.Core;

using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static KiddoCare.Common.RoleConstants;

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
                c.Type,
                c.ParentUserId,
                c.TeacherUserId,
                c.AdminUserId,
                ChildFullName = c.Child == null ? null : c.Child.FirstName + " " + c.Child.LastName,
                c.CreatedOn,
                LastMessageOn = c.Messages.Where(m => !m.IsDeleted && !c.ConversationDeletions.Any(d => d.UserId == userId && m.SentOn <= d.DeletedOn)).OrderByDescending(m => m.SentOn).Select(m => (DateTime?)m.SentOn).FirstOrDefault(),
                LastMessagePreview = c.Messages.Where(m => !m.IsDeleted && !c.ConversationDeletions.Any(d => d.UserId == userId && m.SentOn <= d.DeletedOn)).OrderByDescending(m => m.SentOn).Select(m => m.Content).FirstOrDefault(),
                UnreadMessagesCount = c.Messages.Count(m => !m.IsDeleted && m.SenderUserId != userId && m.ReadOn == null && !c.ConversationDeletions.Any(d => d.UserId == userId && m.SentOn <= d.DeletedOn))
            })
            .OrderByDescending(c => c.LastMessageOn ?? c.CreatedOn)
            .ToListAsync();

        var participantIds = conversations
            .SelectMany(c => new[] { c.ParentUserId, c.TeacherUserId, c.AdminUserId })
            .Where(id => id != null)
            .Select(id => id!)
            .Distinct()
            .ToList();

        var displayNames = await GetDisplayNamesByUserIdsAsync(participantIds);

        return conversations.Select(c => new MessageConversationViewModel
        {
            Id = c.Id,
            ChildFullName = c.ChildFullName,
            ConversationType = GetConversationTypeLabel(c.Type, isAdmin),
            OtherParticipantName = GetOtherParticipantName(c.ParentUserId, c.TeacherUserId, c.AdminUserId, userId, displayNames),
            LastMessagePreview = CreatePreview(c.LastMessagePreview),
            LastMessageOn = c.LastMessageOn,
            UnreadMessagesCount = c.UnreadMessagesCount
        });
    }

    public async Task<int> GetUnreadMessagesCountAsync(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var query = context.Conversations
            .Where(c => !c.IsDeleted &&
                        !c.ConversationDeletions.Any(d => d.UserId == userId && (c.LastMessageOn == null || c.LastMessageOn <= d.DeletedOn)));

        if (isAdmin)
        {
            query = query.Where(c => c.AdminUserId == userId);
        }
        else if (isTeacher)
        {
            query = query.Where(c => c.TeacherUserId == userId);
        }
        else if (isParent)
        {
            query = query.Where(c => c.ParentUserId == userId);
        }
        else
        {
            query = query.Where(c => false);
        }

        return await query
            .SelectMany(c => c.Messages
                .Where(m => !m.IsDeleted &&
                            m.SenderUserId != userId &&
                            m.ReadOn == null &&
                            !c.ConversationDeletions.Any(d => d.UserId == userId && m.SentOn <= d.DeletedOn)))
            .CountAsync();
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
                c.Type,
                c.ParentUserId,
                c.TeacherUserId,
                c.AdminUserId,
                ChildFullName = c.Child == null ? null : c.Child.FirstName + " " + c.Child.LastName,
                DeletedOn = c.ConversationDeletions.Where(d => d.UserId == userId).Select(d => (DateTime?)d.DeletedOn).FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            return null;
        }

        var unreadMessages = await context.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted && m.SenderUserId != userId && m.ReadOn == null && (conversation.DeletedOn == null || m.SentOn > conversation.DeletedOn.Value))
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.ReadOn = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        var messages = await context.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted && (conversation.DeletedOn == null || m.SentOn > conversation.DeletedOn.Value))
            .OrderBy(m => m.SentOn)
            .Select(m => new
            {
                m.Id,
                m.SenderUserId,
                m.Content,
                m.SentOn
            })
            .ToListAsync();

        var participantIds = new[] { conversation.ParentUserId, conversation.TeacherUserId, conversation.AdminUserId }
            .Where(id => id != null)
            .Select(id => id!);

        var senderIds = messages
            .Select(m => m.SenderUserId)
            .Concat(participantIds)
            .Distinct()
            .ToList();

        var displayNames = await GetDisplayNamesByUserIdsAsync(senderIds);

        return new MessageDetailsViewModel
        {
            ConversationId = conversation.Id,
            ChildFullName = conversation.ChildFullName,
            ConversationType = GetConversationTypeLabel(conversation.Type, isAdmin),
            OtherParticipantName = GetOtherParticipantName(conversation.ParentUserId, conversation.TeacherUserId, conversation.AdminUserId, userId, displayNames),
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
        var query = context.Conversations
            .Where(c => c.Id == conversationId &&
                        !c.IsDeleted &&
                        !c.ConversationDeletions.Any(d => d.UserId == userId && (c.LastMessageOn == null || c.LastMessageOn <= d.DeletedOn)));

        if (isAdmin)
        {
            return await query.AnyAsync(c => c.AdminUserId == userId);
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

    public async Task DeleteConversationForUserAsync(int conversationId, string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var conversation = await context.Conversations
            .Where(c => c.Id == conversationId && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.ParentUserId,
                c.TeacherUserId,
                c.AdminUserId
            })
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found.");
        }

        bool isParticipant = IsConversationParticipant(conversation.ParentUserId, conversation.TeacherUserId, conversation.AdminUserId, userId, isAdmin, isTeacher, isParent);

        if (!isParticipant)
        {
            throw new UnauthorizedAccessException();
        }

        var deletion = await context.ConversationDeletions
            .FirstOrDefaultAsync(d => d.ConversationId == conversationId && d.UserId == userId);

        if (deletion == null)
        {
            await context.ConversationDeletions.AddAsync(new ConversationDeletion
            {
                ConversationId = conversationId,
                UserId = userId,
                DeletedOn = DateTime.UtcNow
            });
        }
        else
        {
            deletion.DeletedOn = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
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

    public async Task<MessageCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        return new MessageCreateViewModel
        {
            Recipients = await GetRecipientItemsAsync(userId, isAdmin, isTeacher, isParent),
            Children = new List<SelectListItem>()
        };
    }

    public async Task<int> CreateConversationAsync(MessageCreateViewModel model, string senderUserId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var recipientUserId = model.RecipientUserId;
        bool recipientIsAdmin = await IsUserInRoleAsync(recipientUserId, Admin);
        bool recipientIsTeacher = await context.TeacherProfiles.AnyAsync(t => t.UserId == recipientUserId && !t.IsDeleted);
        bool recipientIsParent = await context.ParentProfiles.AnyAsync(p => p.UserId == recipientUserId && !p.IsDeleted);

        var conversation = await BuildConversationAsync(model.ChildId, senderUserId, recipientUserId, isAdmin, isTeacher, isParent, recipientIsAdmin, recipientIsTeacher, recipientIsParent);
        var existingConversation = await FindExistingConversationAsync(conversation);
        var sentOn = DateTime.UtcNow;

        if (existingConversation == null)
        {
            conversation.CreatedOn = sentOn;
            conversation.LastMessageOn = sentOn;
            conversation.Messages.Add(new ChatMessage
            {
                SenderUserId = senderUserId,
                Content = model.Content.Trim(),
                SentOn = sentOn
            });

            await context.Conversations.AddAsync(conversation);
            await context.SaveChangesAsync();

            return conversation.Id;
        }

        await context.ChatMessages.AddAsync(new ChatMessage
        {
            ConversationId = existingConversation.Id,
            SenderUserId = senderUserId,
            Content = model.Content.Trim(),
            SentOn = sentOn
        });

        existingConversation.LastMessageOn = sentOn;
        await context.SaveChangesAsync();

        return existingConversation.Id;
    }

    public async Task<IEnumerable<SelectListItem>> GetAvailableChildrenAsync(string userId, string recipientUserId, bool isAdmin, bool isTeacher, bool isParent)
    {
        bool recipientIsAdmin = await IsUserInRoleAsync(recipientUserId, Admin);
        bool recipientIsTeacher = await context.TeacherProfiles.AnyAsync(t => t.UserId == recipientUserId && !t.IsDeleted);
        bool recipientIsParent = await context.ParentProfiles.AnyAsync(p => p.UserId == recipientUserId && !p.IsDeleted);

        if (isAdmin && recipientIsParent)
        {
            return await GetChildrenForParentAsync(recipientUserId);
        }

        if (isAdmin && recipientIsTeacher)
        {
            return await GetChildrenForTeacherAsync(recipientUserId);
        }

        if (isTeacher && recipientIsParent)
        {
            return await GetChildrenForParentAndTeacherAsync(recipientUserId, userId);
        }

        if (isTeacher && recipientIsAdmin)
        {
            return await GetChildrenForTeacherAsync(userId);
        }

        if (isParent && recipientIsTeacher)
        {
            return await GetChildrenForParentAndTeacherAsync(userId, recipientUserId);
        }

        if (isParent && recipientIsAdmin)
        {
            return await GetChildrenForParentAsync(userId);
        }

        return new List<SelectListItem>();
    }

    private IQueryable<Conversation> GetAccessibleConversations(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var query = context.Conversations
            .Include(c => c.Child)
                .ThenInclude(c => c!.Group)
            .Include(c => c.ParentUser)
            .Include(c => c.TeacherUser)
            .Include(c => c.AdminUser)
            .Include(c => c.Messages)
            .Where(c => !c.IsDeleted && !c.ConversationDeletions.Any(d => d.UserId == userId && (c.LastMessageOn == null || c.LastMessageOn <= d.DeletedOn)));

        if (isAdmin)
        {
            return query.Where(c => c.AdminUserId == userId);
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

    private static bool IsConversationParticipant(string? parentUserId, string? teacherUserId, string? adminUserId, string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        if (isAdmin)
        {
            return adminUserId == userId;
        }

        if (isTeacher)
        {
            return teacherUserId == userId;
        }

        if (isParent)
        {
            return parentUserId == userId;
        }

        return false;
    }

    private async Task<IEnumerable<SelectListItem>> GetRecipientItemsAsync(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var recipientIds = new List<string>();
        var labels = new Dictionary<string, string>();

        if (isAdmin)
        {
            var teachers = await context.TeacherProfiles
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.FullName)
                .Select(t => new { t.UserId, t.FullName })
                .ToListAsync();

            var parents = await context.ParentProfiles
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.FullName)
                .Select(p => new { p.UserId, p.FullName })
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                recipientIds.Add(teacher.UserId);
                labels[teacher.UserId] = $"{teacher.FullName} ({GetRoleDisplayName(Teacher)})";
            }

            foreach (var parent in parents)
            {
                recipientIds.Add(parent.UserId);
                labels[parent.UserId] = $"{parent.FullName} ({GetRoleDisplayName(Parent)})";
            }
        }
        else if (isTeacher)
        {
            var adminIds = await GetRoleUserIdsAsync(Admin);
            var groupIds = await context.TeacherProfiles
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .Select(t => t.GroupId)
                .ToListAsync();

            var parentIds = await context.Children
                .Where(c => !c.IsDeleted && groupIds.Contains(c.GroupId) && c.Parent != null)
                .Select(c => c.Parent!.UserId)
                .Distinct()
                .ToListAsync();

            recipientIds.AddRange(adminIds);
            recipientIds.AddRange(parentIds);
        }
        else if (isParent)
        {
            var adminIds = await GetRoleUserIdsAsync(Admin);
            var teacherIds = await context.Children
                .Where(c => !c.IsDeleted && c.Parent != null && c.Parent.UserId == userId && c.Group != null)
                .SelectMany(c => c.Group!.Teachers.Where(t => !t.IsDeleted).Select(t => t.UserId))
                .Distinct()
                .ToListAsync();

            recipientIds.AddRange(adminIds);
            recipientIds.AddRange(teacherIds);
        }

        recipientIds = recipientIds.Where(id => id != userId).Distinct().ToList();
        var displayNames = await GetDisplayNamesByUserIdsAsync(recipientIds);

        var items = recipientIds
            .Select(id => new SelectListItem(labels.GetValueOrDefault(id, displayNames.GetValueOrDefault(id, "Unknown user")), id))
            .ToList();

        return isAdmin ? items : items.OrderBy(i => i.Text).ToList();
    }

    private async Task<IEnumerable<SelectListItem>> GetChildItemsAsync(string userId, bool isAdmin, bool isTeacher, bool isParent)
    {
        var query = context.Children
            .Include(c => c.Group)
            .Include(c => c.Parent)
            .Where(c => !c.IsDeleted);

        if (isTeacher)
        {
            var groupIds = await context.TeacherProfiles
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .Select(t => t.GroupId)
                .ToListAsync();

            query = query.Where(c => groupIds.Contains(c.GroupId));
        }
        else if (isParent)
        {
            query = query.Where(c => c.Parent != null && c.Parent.UserId == userId);
        }
        else if (!isAdmin)
        {
            query = query.Where(c => false);
        }

        return await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem(c.FirstName + " " + c.LastName + " - " + c.Group!.Name, c.Id.ToString()))
            .ToListAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetChildrenForParentAsync(string parentUserId)
    {
        return await context.Children
            .Include(c => c.Group)
            .Where(c => !c.IsDeleted && c.Parent != null && c.Parent.UserId == parentUserId)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem(c.FirstName + " " + c.LastName + " - " + c.Group!.Name, c.Id.ToString()))
            .ToListAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetChildrenForTeacherAsync(string teacherUserId)
    {
        var groupIds = await context.TeacherProfiles
            .Where(t => t.UserId == teacherUserId && !t.IsDeleted)
            .Select(t => t.GroupId)
            .ToListAsync();

        return await context.Children
            .Include(c => c.Group)
            .Where(c => !c.IsDeleted && groupIds.Contains(c.GroupId))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem(c.FirstName + " " + c.LastName + " - " + c.Group!.Name, c.Id.ToString()))
            .ToListAsync();
    }

    private async Task<IEnumerable<SelectListItem>> GetChildrenForParentAndTeacherAsync(string parentUserId, string teacherUserId)
    {
        return await context.Children
            .Include(c => c.Group)
            .Where(c => !c.IsDeleted &&
                        c.Parent != null &&
                        c.Parent.UserId == parentUserId &&
                        c.Group != null &&
                        c.Group.Teachers.Any(t => !t.IsDeleted && t.UserId == teacherUserId))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Select(c => new SelectListItem(c.FirstName + " " + c.LastName + " - " + c.Group!.Name, c.Id.ToString()))
            .ToListAsync();
    }

    private async Task<Conversation> BuildConversationAsync(int? childId, string senderUserId, string recipientUserId, bool isAdmin, bool isTeacher, bool isParent, bool recipientIsAdmin, bool recipientIsTeacher, bool recipientIsParent)
    {
        if (isAdmin && recipientIsParent)
        {
            return new Conversation
            {
                Type = ConversationType.ParentAdmin,
                ChildId = await ValidateOptionalParentChildAsync(childId, recipientUserId),
                ParentUserId = recipientUserId,
                AdminUserId = senderUserId
            };
        }

        if (isAdmin && recipientIsTeacher)
        {
            return new Conversation
            {
                Type = ConversationType.TeacherAdmin,
                ChildId = await ValidateOptionalTeacherChildAsync(childId, recipientUserId),
                TeacherUserId = recipientUserId,
                AdminUserId = senderUserId
            };
        }

        if (isTeacher && recipientIsAdmin)
        {
            return new Conversation
            {
                Type = ConversationType.TeacherAdmin,
                ChildId = await ValidateOptionalTeacherChildAsync(childId, senderUserId),
                TeacherUserId = senderUserId,
                AdminUserId = recipientUserId
            };
        }

        if (isTeacher && recipientIsParent)
        {
            return new Conversation
            {
                Type = ConversationType.ParentTeacher,
                ChildId = await ValidateParentTeacherChildAsync(childId, recipientUserId, senderUserId),
                ParentUserId = recipientUserId,
                TeacherUserId = senderUserId
            };
        }

        if (isParent && recipientIsAdmin)
        {
            return new Conversation
            {
                Type = ConversationType.ParentAdmin,
                ChildId = await ValidateOptionalParentChildAsync(childId, senderUserId),
                ParentUserId = senderUserId,
                AdminUserId = recipientUserId
            };
        }

        if (isParent && recipientIsTeacher)
        {
            return new Conversation
            {
                Type = ConversationType.ParentTeacher,
                ChildId = await ValidateParentTeacherChildAsync(childId, senderUserId, recipientUserId),
                ParentUserId = senderUserId,
                TeacherUserId = recipientUserId
            };
        }

        throw new InvalidOperationException("You cannot start a conversation with this recipient.");
    }

    private async Task<int?> ValidateOptionalParentChildAsync(int? childId, string parentUserId)
    {
        if (childId == null)
        {
            return null;
        }

        bool canUseChild = await context.Children
            .AnyAsync(c => !c.IsDeleted && c.Id == childId.Value && c.Parent != null && c.Parent.UserId == parentUserId);

        if (!canUseChild)
        {
            throw new InvalidOperationException("Selected child is not available for this conversation.");
        }

        return childId.Value;
    }

    private async Task<int?> ValidateOptionalTeacherChildAsync(int? childId, string teacherUserId)
    {
        if (childId == null)
        {
            return null;
        }

        bool canUseChild = await context.Children
            .AnyAsync(c => !c.IsDeleted &&
                           c.Id == childId.Value &&
                           c.Group != null &&
                           c.Group.Teachers.Any(t => !t.IsDeleted && t.UserId == teacherUserId));

        if (!canUseChild)
        {
            throw new InvalidOperationException("Selected child is not available for this conversation.");
        }

        return childId.Value;
    }

    private async Task<int> ValidateParentTeacherChildAsync(int? childId, string parentUserId, string teacherUserId)
    {
        if (childId == null)
        {
            throw new InvalidOperationException("Please select a child for parent-teacher conversations.");
        }

        bool canUseChild = await context.Children
            .AnyAsync(c => !c.IsDeleted &&
                           c.Id == childId.Value &&
                           c.Parent != null &&
                           c.Parent.UserId == parentUserId &&
                           c.Group != null &&
                           c.Group.Teachers.Any(t => !t.IsDeleted && t.UserId == teacherUserId));

        if (!canUseChild)
        {
            throw new InvalidOperationException("Selected child is not available for this conversation.");
        }

        return childId.Value;
    }

    private async Task<Conversation?> FindExistingConversationAsync(Conversation conversation)
    {
        return await context.Conversations.FirstOrDefaultAsync(c =>
            !c.IsDeleted &&
            c.Type == conversation.Type &&
            c.ChildId == conversation.ChildId &&
            c.ParentUserId == conversation.ParentUserId &&
            c.TeacherUserId == conversation.TeacherUserId &&
            c.AdminUserId == conversation.AdminUserId);
    }

    private async Task<bool> IsUserInRoleAsync(string userId, string role)
    {
        return await context.UserRoles
            .Join(context.Roles, userRole => userRole.RoleId, identityRole => identityRole.Id, (userRole, identityRole) => new { userRole.UserId, identityRole.Name })
            .AnyAsync(r => r.UserId == userId && r.Name == role);
    }

    private async Task<List<string>> GetRoleUserIdsAsync(string role)
    {
        return await context.UserRoles
            .Join(context.Roles, userRole => userRole.RoleId, identityRole => identityRole.Id, (userRole, identityRole) => new { userRole.UserId, identityRole.Name })
            .Where(r => r.Name == role)
            .Select(r => r.UserId)
            .ToListAsync();
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

    private static string GetOtherParticipantName(string? parentUserId, string? teacherUserId, string? adminUserId, string currentUserId, Dictionary<string, string> displayNames)
    {
        string? otherUserId = new[] { parentUserId, teacherUserId, adminUserId }
            .FirstOrDefault(id => id != null && id != currentUserId);

        return otherUserId == null
            ? "Unknown user"
            : displayNames.GetValueOrDefault(otherUserId, "Unknown user");
    }

    private static string GetConversationTypeLabel(ConversationType type, bool isAdmin)
    {
        return type switch
        {
            ConversationType.ParentTeacher => "Parent-teacher conversation",
            ConversationType.ParentAdmin => isAdmin ? "Parent conversation" : "Administration conversation",
            ConversationType.TeacherAdmin => isAdmin ? "Teacher conversation" : "Administration conversation",
            _ => "Conversation"
        };
    }

    private static string GetRoleDisplayName(string role)
    {
        bool isBulgarian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "bg";

        return role switch
        {
            Teacher => isBulgarian ? "Учител" : "Teacher",
            Parent => isBulgarian ? "Родител" : "Parent",
            _ => role
        };
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
