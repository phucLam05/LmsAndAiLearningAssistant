using BLL.Interfaces;
using Core.DTOs.Admin;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly IAuthService _authService;

        public AdminController(IAdminService adminService, IUserService userService, IAuthService authService)
        {
            _adminService = adminService;
            _userService = userService;
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
        public async Task<IActionResult> Index()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? search = null, string? role = null)
        {
            var dbUsers = await _adminService.GetAllUsersAsync();
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
                    StudentCode = u.UserCode ?? string.Empty,
                    Status = u.Status.ToString(),
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
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string fullName, string email, string role, string? studentCode, bool mustChangePassword = false)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Name and Email are required.";
                return RedirectToAction(nameof(Users));
            }

            var userRole = Enum.TryParse<UserRole>(role, true, out var parsedRole) ? parsedRole : UserRole.Student;
            
            // If StudentCode is empty, generate a random one
            if (string.IsNullOrWhiteSpace(studentCode))
            {
                studentCode = "STU" + new Random().Next(100000, 999999).ToString();
            }

            var createDto = new UserCreateDto
            {
                Email = email,
                FullName = fullName,
                Role = userRole,
                UserCode = studentCode
            };

            var result = await _userService.CreateUserAsync(createDto);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
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
            var userStatus = Enum.TryParse<UserStatus>(status, true, out var parsedStatus) ? parsedStatus : UserStatus.Active;
            
            var editDto = new UserEditDto
            {
                Id = id,
                FullName = fullName,
                Role = userRole,
                Status = userStatus
            };

            var result = await _userService.UpdateUserAsync(editDto);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
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
            var result = await _userService.DeleteUserAsync(id);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "User has been deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
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

                var code = string.IsNullOrWhiteSpace(row.StudentCode) ? "STU" + new Random().Next(100000, 999999).ToString() : row.StudentCode;

                var createDto = new UserCreateDto
                {
                    Email = row.Email,
                    FullName = row.FullName,
                    Role = UserRole.Student,
                    UserCode = code
                };

                var result = await _userService.CreateUserAsync(createDto);
                if (result.IsSuccess)
                {
                    successCount++;
                    results.Add(new { name = row.FullName, email = row.Email, status = "Success", studentCode = code });
                }
                else
                {
                    failCount++;
                    results.Add(new { name = row.FullName, email = row.Email, status = "Failed", reason = result.ErrorMessage });
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
        }
    }
}
