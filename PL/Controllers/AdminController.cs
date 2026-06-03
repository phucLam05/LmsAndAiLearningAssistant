using BLL.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return View(stats);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _adminService.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(Guid userId, UserRole newRole)
        {
            var success = await _adminService.ChangeUserRoleAsync(userId, newRole);
            if (!success)
            {
                TempData["ErrorMessage"] = "Failed to update user role.";
            }
            else
            {
                TempData["SuccessMessage"] = "User role updated successfully.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
