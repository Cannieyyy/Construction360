using Construction360.Enums;
using Construction360.Models;
using Construction360.Repositories;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Construction360.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;

        public AccountController()
        {
            _userRepository = new UserRepository();
        }

        public IActionResult Login()
        {
            // If already logged in, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userRepository.AuthenticateAsync(model.Email, model.Password);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated. Please contact administrator.");
                    return View(model);
                }

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("EmployeeId", user.EmployeeId ?? ""),
                    new Claim("Initials", GetInitials(user.FullName)),
                    new Claim("Department", user.Department ?? ""),
                    new Claim("Position", user.Position ?? "")
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
                });

                // Redirect based on role
                return RedirectToDashboard();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate password
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return View(model);
            }

            // Validate password strength
            if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
                return View(model);
            }

            try
            {
                // Check if user already exists
                if (await _userRepository.UserExistsAsync(model.Email, model.Username))
                {
                    ModelState.AddModelError("", "An account with this email or username already exists.");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Username = model.Username,
                    Email = model.Email,
                    Role = model.Role,
                    Department = model.Department,
                    Position = model.Position,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var result = await _userRepository.CreateUserAsync(user, model.Password);

                if (!result)
                {
                    ModelState.AddModelError("", "Failed to create account. Please try again.");
                    return View(model);
                }

                // Add success notification
                TempData["SuccessMessage"] = "Account created successfully! Please login.";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToDashboard()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Supervisor" => RedirectToAction("Dashboard", "Supervisor"),
                "Employee" => RedirectToAction("Dashboard", "Employee"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "U";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "U";

            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
        }
    }
}