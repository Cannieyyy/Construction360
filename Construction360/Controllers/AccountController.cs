using Construction360.Enums;
using Construction360.Models;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Construction360.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var user = MockData.Authenticate(model.Email, model.Password);
            if (user == null)
            {
                model.Error = "Invalid email or password.";
                return View(model);
            }

            var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("EmployeeId", user.EmployeeId),
            new("UserId", user.Id.ToString()),
            new("Initials", user.Initials),
        };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("Cookies", principal);

            return user.Role switch
            {
                UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                UserRole.Supervisor => RedirectToAction("Dashboard", "Supervisor"),
                UserRole.Employee => RedirectToAction("Dashboard", "Employee"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                model.Error = "Passwords do not match.";
                return View(model);
            }
            if (MockData.Users.Any(u => u.Email == model.Email))
            {
                model.Error = "An account with this email already exists.";
                return View(model);
            }

            var newUser = new User
            {
                Id = MockData.Users.Count + 1,
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PasswordHash = model.Password,
                Role = model.Role,
                EmployeeId = $"EMP-{DateTime.Now.Year}-{(MockData.Users.Count + 1):D3}"
            };
            MockData.Users.Add(newUser);
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }
    }
}
