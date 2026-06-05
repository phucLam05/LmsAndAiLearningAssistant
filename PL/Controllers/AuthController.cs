using BLL.Interfaces;
using Core.DTOs.Auth;
using Core.Entities;
using DAL.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PL.Models.Auth;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    /// <summary>
    /// Controller responsible for handling user authentication (Login, Logout, First-time password change).
    /// </summary>
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _dbContext;

        public AuthController(IAuthService authService, ApplicationDbContext dbContext)
        {
            _authService = authService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            return NotFound();
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Subject");
            }
            
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var loginDto = new LoginDto
            {
                Email = model.Email,
                Password = model.Password
            };

            var user = await _authService.LoginAsync(loginDto);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Check if user is inactive (0) which indicates mandatory password change
            if (user.Status == UserStatus.Inactive)
            {
                return RedirectToAction(nameof(ChangePassword), new { userId = user.Id, email = model.Email });
            }

            // Create security claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Subject");
        }

        [HttpGet]
        public IActionResult ChangePassword(Guid userId, string email)
        {
            if (userId == Guid.Empty || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewBag.Email = email;
            return View(new FirstTimeChangePasswordDto { UserId = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(FirstTimeChangePasswordDto model, string email)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Email = email;
                return View(model);
            }

            var result = await _authService.ActivateAccountAsync(model.UserId, model.TemporaryPassword, model.NewPassword);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                ViewBag.Email = email;
                return View(model);
            }

            var user = await _dbContext.Users.FindAsync(model.UserId);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in immediately after successful activation
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Account activated and password changed successfully.";
            return RedirectToAction("Index", "Subject");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
