using Construction360.Enums;
using Construction360.Models;
using Construction360.Repositories;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Construction360.Controllers.Api
{
    /// <summary>
    /// Authentication API endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public AuthApiController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="model">Login credentials</param>
        /// <returns>User information and authentication cookie</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var user = await _userRepository.AuthenticateAsync(model.Email, model.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is deactivated" });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Initials", GetInitials(user.FullName))
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            return Ok(new
            {
                message = "Login successful",
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role = user.Role.ToString()
                }
            });
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="model">Registration details</param>
        /// <returns>Created user info</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            if (await _userRepository.UserExistsAsync(model.Email, model.Username))
                return BadRequest(new { message = "User already exists" });

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
                return BadRequest(new { message = "Failed to create user" });

            return CreatedAtAction(nameof(Login), new { id = user.Id }, new
            {
                message = "User created successfully",
                user = new { id = user.Id, email = user.Email }
            });
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Get current logged-in user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                userId = User.FindFirst("UserId")?.Value,
                fullName = User.Identity?.Name,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                role = User.FindFirst(ClaimTypes.Role)?.Value
            });
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "U";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
    }
}