using Microsoft.AspNetCore.Mvc;
using PL.Models;
using System.Diagnostics;

namespace PL.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Subject");
                if (User.IsInRole("Lecturer"))
                    return RedirectToAction("MySubjects", "Subject");
                
                return RedirectToAction("Browse", "Subject");
            }
            return RedirectToAction("Login", "Auth");
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
