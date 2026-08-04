using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class ActivityFeedController : Controller
{
    private readonly IActivityFeedService activityFeedService;

    public ActivityFeedController(IActivityFeedService activityFeedService)
    {
        this.activityFeedService = activityFeedService;
    }

    [HttpGet]
    public async Task<IActionResult> Child(int childId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await activityFeedService.GetChildFeedAsync(childId, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }
}