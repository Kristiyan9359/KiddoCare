using KiddoCare.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace KiddoCare.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;

            return View();
        }

        public IActionResult StatusCode(int code)
        {
            Response.StatusCode = code;

            ViewBag.StatusCode = code;
            ViewBag.Title = code switch
            {
                404 => "Page not found",
                403 => "Access denied",
                _ => "Something went wrong"
            };

            ViewBag.Message = code switch
            {
                404 => "The page you are looking for does not exist or has been moved.",
                403 => "You do not have permission to access this page.",
                _ => "The request could not be completed."
            };

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
