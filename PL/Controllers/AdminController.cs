using BLL.Interfaces;
using Core.DTOs.Admin;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PL.Controllers
{
    /// <summary>
    /// Controller for Admin-only dashboard statistics, manual User CRUD, and bulk importing.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;

        public AdminController(IAdminService adminService, IUserService userService)
        {
            _adminService = adminService;
            _userService = userService;
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

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.CreateUserAsync(model);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            TempData["SuccessMessage"] = $"User {model.FullName} created successfully and notification sent.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(Guid id)
        {
            var users = await _adminService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var editDto = new UserEditDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Role = user.Role,
                Status = user.Status
            };

            return View(editDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserEditDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.UpdateUserAsync(model);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            TempData["SuccessMessage"] = $"User updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStudents(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid Excel file.";
                return RedirectToAction(nameof(Users));
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                TempData["ErrorMessage"] = "Only Excel files (.xlsx) are supported.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                var result = await _userService.ImportStudentsFromExcelAsync(stream);
                
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }
                else
                {
                    TempData["SuccessMessage"] = $"Successfully imported {result.Data} students!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"System error during import: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
