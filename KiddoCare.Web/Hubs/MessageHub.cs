namespace KiddoCare.Web.Hubs;

using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using static KiddoCare.Common.RoleConstants;

[Authorize(Roles = $"{Admin},{Teacher},{Parent}")]
public class MessageHub : Hub
{
    private readonly IMessageService messageService;

    public MessageHub(IMessageService messageService)
    {
        this.messageService = messageService;
    }

    public static string GetConversationGroupName(int conversationId)
        => $"conversation-{conversationId}";

    public static string GetUserGroupName(string userId)
        => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(int conversationId)
    {
        var userId = GetCurrentUserId();
        var (isAdmin, isTeacher, isParent) = GetCurrentUserRoles();

        var canAccess = await messageService.CanAccessConversationAsync(conversationId, userId, isAdmin, isTeacher, isParent);

        if (!canAccess)
        {
            throw new HubException("Conversation is not available.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
    }

    public async Task MarkConversationAsRead(int conversationId)
    {
        var userId = GetCurrentUserId();
        var (isAdmin, isTeacher, isParent) = GetCurrentUserRoles();

        await messageService.MarkConversationAsReadAsync(conversationId, userId, isAdmin, isTeacher, isParent);

        var unreadMessagesCount = await messageService.GetUnreadMessagesCountAsync(userId, isAdmin, isTeacher, isParent);

        await Clients
            .Group(GetUserGroupName(userId))
            .SendAsync("UnreadMessagesCountUpdated", unreadMessagesCount);
    }

    private string GetCurrentUserId()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("User is not authenticated.");
        }

        return userId;
    }

    private (bool IsAdmin, bool IsTeacher, bool IsParent) GetCurrentUserRoles()
    {
        return (
            Context.User!.IsInRole(Admin),
            Context.User.IsInRole(Teacher),
            Context.User.IsInRole(Parent));
    }
}
