namespace KiddoCare.Web.Controllers;

using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.Messages;
using KiddoCare.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.RoleConstants;

[Authorize(Roles = $"{Admin},{Teacher},{Parent}")]
public class MessagesController : Controller
{
    private readonly IMessageService messageService;
    private readonly IStringLocalizer<SharedResource> localizer;

    public MessagesController(IMessageService messageService, IStringLocalizer<SharedResource> localizer)
    {
        this.messageService = messageService;
        this.localizer = localizer;
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

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isParent = User.IsInRole(Parent);

        var model = await messageService.GetCreateModelAsync(userId, isAdmin, isTeacher, isParent);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MessageCreateViewModel model)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);
        var isParent = User.IsInRole(Parent);

        if (!ModelState.IsValid)
        {
            var createModel = await messageService.GetCreateModelAsync(userId, isAdmin, isTeacher, isParent);
            model.Recipients = createModel.Recipients;
            model.Children = createModel.Children;

            return View(model);
        }

        try
        {
            var conversationId = await messageService.CreateConversationAsync(model, userId, isAdmin, isTeacher, isParent);
            this.SetSuccessMessage("Message sent successfully.");

            return RedirectToAction(nameof(Details), new { id = conversationId });
        }
        catch (InvalidOperationException ex)
        {
            var createModel = await messageService.GetCreateModelAsync(userId, isAdmin, isTeacher, isParent);
            model.Recipients = createModel.Recipients;
            model.Children = createModel.Children;
            ModelState.AddModelError(string.Empty, this.localizer[ex.Message]);

            return View(model);
        }
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
