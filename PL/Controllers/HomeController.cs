using Microsoft.AspNetCore.Mvc;
using PL.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace PL.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                if (role == "Admin") return RedirectToAction("Index", "Admin");
                if (role == "Lecturer") return RedirectToAction("MySubjects", "Subject");
                if (role == "Student") return RedirectToAction("Browse", "Subject");
                return RedirectToAction("Index", "Subject");
            }
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
