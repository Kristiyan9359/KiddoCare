namespace KiddoCare.Web.Areas.Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    public IActionResult OnGet()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return Page();
    }

    public IActionResult OnPost()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return Page();
    }
}
