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

    public async Task JoinConversation(int conversationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("User is not authenticated.");
        }

        var isAdmin = Context.User!.IsInRole(Admin);
        var isTeacher = Context.User.IsInRole(Teacher);
        var isParent = Context.User.IsInRole(Parent);

        var canAccess = await messageService.CanAccessConversationAsync(conversationId, userId, isAdmin, isTeacher, isParent);

        if (!canAccess)
        {
            throw new HubException("Conversation is not available.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
    }
}
