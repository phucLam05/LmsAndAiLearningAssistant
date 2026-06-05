<<<<<<< Updated upstream
using BLL.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
=======
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Interfaces;
using Core.DTOs.Auth;
using Core.Entities;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
>>>>>>> Stashed changes

namespace PL.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
<<<<<<< Updated upstream
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
=======
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AdminController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public class MockUser
        {
            public Guid Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = "Student"; // Admin, Lecturer, Student
            public string StudentCode { get; set; } = string.Empty;
            public string Status { get; set; } = "Active"; // Active (status=1), Inactive (status=0)
            public DateTime CreatedAt { get; set; }
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? search = null, string? role = null)
        {
            var dbUsers = await _context.Users.ToListAsync();
            var users = new List<MockUser>();

            foreach (var u in dbUsers)
            {
                string email = "N/A";
                try
                {
                    email = _authService.DecryptEmail(u.EmailEncrypt);
                }
                catch
                {
                    email = u.EmailHash;
                }

                users.Add(new MockUser
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = email,
                    Role = u.Role.ToString(),
                    StudentCode = "", // Supabase table does not store student code
                    Status = "Active",
                    CreatedAt = u.CreatedAt
                });
            }

            var filteredUsers = users.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                filteredUsers = filteredUsers.Where(u => u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                                         u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(role))
            {
                filteredUsers = filteredUsers.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.Search = search;
            ViewBag.Role = role;

            return View(filteredUsers.OrderByDescending(u => u.CreatedAt).ToList());
>>>>>>> Stashed changes
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
<<<<<<< Updated upstream
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
=======
        public async Task<IActionResult> CreateUser(string fullName, string email, string role, string? studentCode, bool mustChangePassword = false)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Name and Email are required.";
                return RedirectToAction(nameof(Users));
            }

            var userRole = Enum.TryParse<UserRole>(role, true, out var parsedRole) ? parsedRole : UserRole.Student;
            var registerDto = new RegisterDto
            {
                Email = email,
                Password = "LmsPassword123!", // Temp default password
                ConfirmPassword = "LmsPassword123!",
                FullName = fullName,
                Role = userRole
            };

            var (success, errorMessage) = await _authService.RegisterAsync(registerDto);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"User {fullName} has been created successfully!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(Guid id, string fullName, string email, string role, string? studentCode, string status)
        {
            var userRole = Enum.TryParse<UserRole>(role, true, out var parsedRole) ? parsedRole : UserRole.Student;
            var (success, errorMessage) = await _authService.UpdateUserAsync(id, fullName, email, userRole);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"User {fullName} has been updated successfully!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var success = await _authService.DeleteUserAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "User has been deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> BulkImport([FromBody] List<ImportRow> rows)
        {
            if (rows == null || !rows.Any())
            {
                return Json(new { success = false, message = "No data received." });
            }

            int successCount = 0;
            int failCount = 0;
            var results = new List<object>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Email) || string.IsNullOrWhiteSpace(row.FullName))
                {
                    failCount++;
                    results.Add(new { name = row.FullName, email = row.Email, status = "Failed", reason = "Missing FullName or Email" });
                    continue;
                }

                if (!row.Email.Contains("@"))
                {
                    failCount++;
                    results.Add(new { name = row.FullName, email = row.Email, status = "Failed", reason = "Invalid Email Format" });
                    continue;
                }

                var registerDto = new RegisterDto
                {
                    Email = row.Email,
                    Password = "LmsPassword123!",
                    ConfirmPassword = "LmsPassword123!",
                    FullName = row.FullName,
                    Role = UserRole.Student
                };

                var (success, errorMessage) = await _authService.RegisterAsync(registerDto);
                if (success)
                {
                    successCount++;
                    results.Add(new { name = row.FullName, email = row.Email, status = "Success", studentCode = row.StudentCode });
                }
                else
                {
                    failCount++;
                    results.Add(new { name = row.FullName, email = row.Email, status = "Failed", reason = errorMessage });
                }
            }

            return Json(new 
            { 
                success = true, 
                successCount, 
                failCount, 
                results 
            });
        }

        public class ImportRow
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string StudentCode { get; set; } = string.Empty;
>>>>>>> Stashed changes
        }
    }
}
