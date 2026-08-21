using KiddoCare.ViewModels.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KiddoCare.Services.Core.Contracts;

public interface IMessageService
{
    Task<IEnumerable<MessageConversationViewModel>> GetConversationsAsync(string userId, bool isAdmin, bool isTeacher, bool isParent);

    Task<int> GetUnreadMessagesCountAsync(string userId, bool isAdmin, bool isTeacher, bool isParent);

    Task<MessageDetailsViewModel?> GetDetailsAsync(int conversationId, string userId, bool isAdmin, bool isTeacher, bool isParent);

    Task<bool> CanAccessConversationAsync(int conversationId, string userId, bool isAdmin, bool isTeacher, bool isParent);

    Task SendMessageAsync(SendMessageInputModel model, string senderUserId, bool isAdmin, bool isTeacher, bool isParent);

    Task DeleteConversationForUserAsync(int conversationId, string userId, bool isAdmin, bool isTeacher, bool isParent);

    Task<MessageCreateViewModel> GetCreateModelAsync(string userId, bool isAdmin, bool isTeacher, bool isParent);

    Task<int> CreateConversationAsync(MessageCreateViewModel model, string senderUserId, bool isAdmin, bool isTeacher, bool isParent);

    Task<IEnumerable<SelectListItem>> GetAvailableChildrenAsync(string userId, string recipientUserId, bool isAdmin, bool isTeacher, bool isParent);
}
