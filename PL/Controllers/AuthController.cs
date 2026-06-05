using BLL.Interfaces;
using Core.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PL.Models.Auth;
using System.Security.Claims;

namespace PL.Controllers
{
    /// <summary>
    /// Controller responsible for handling user authentication (Registration, Login, Logout).
    /// Acts as an intermediary between the views and the Business Access Layer (BAL).
    /// </summary>
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            // If already logged in, redirect to Drive
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Subjects", "Drive");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var registerDto = new RegisterDto
            {
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                FullName = model.FullName,
                Role = model.Role
            };

            var (success, errorMessage) = await _authService.RegisterAsync(registerDto);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }

            // On success, redirect to login page with a success message
            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Subjects", "Drive");
            }
            
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, bool simulateFirstTime = false)
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
                IsPersistent = true, // Keep the user logged in across browser sessions
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Sign in the user using Cookie Authentication
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            if (simulateFirstTime)
            {
                Response.Cookies.Append("MustChangePassword", "true");
                return RedirectToAction(nameof(ForceChangePassword));
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Subjects", "Drive");
        }

        [HttpGet]
        public IActionResult ForceChangePassword()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Login));
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForceChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Password must be at least 6 characters.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation do not match.");
                return View();
            }

            // Successfully changed password! Clear the force change cookie.
            Response.Cookies.Delete("MustChangePassword");
            TempData["SuccessMessage"] = "Your password has been changed successfully! Welcome to LMS AI.";
            return RedirectToAction("Index", "Home");
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
