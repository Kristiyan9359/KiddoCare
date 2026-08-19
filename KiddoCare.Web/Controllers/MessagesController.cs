namespace KiddoCare.Web.Controllers;

using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Messages;
using KiddoCare.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

[Authorize]
public class MessagesController : Controller
{
    private readonly IMessageService messageService;

    public MessagesController(IMessageService messageService)
    {
        this.messageService = messageService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isParent = User.IsInRole(Parent);

        var conversations = await messageService.GetConversationsAsync(userId, isAdmin, isTeacher, isParent);

        return View(conversations);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isParent = User.IsInRole(Parent);

        var model = await messageService.GetDetailsAsync(id, userId, isAdmin, isTeacher, isParent);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendMessageInputModel model)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isParent = User.IsInRole(Parent);

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id = model.ConversationId });
        }

        try
        {
            await messageService.SendMessageAsync(model, userId, isAdmin, isTeacher, isParent);
            this.SetSuccessMessage("Message sent successfully.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Details), new { id = model.ConversationId });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}