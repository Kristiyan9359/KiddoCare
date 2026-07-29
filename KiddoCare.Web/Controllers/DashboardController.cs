using System.Security.Claims;
using static KiddoCare.Common.RoleConstants;
using KiddoCare.Services.Core.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (User.IsInRole(Parent))
        {
            var parentModel = await dashboardService.GetParentDashboardAsync(userId);
            return View("Parent", parentModel);
        }

        var model = await dashboardService.GetDashboardAsync();

        return View(model);
    }
}