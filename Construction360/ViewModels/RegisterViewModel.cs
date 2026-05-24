using Construction360.Enums;

namespace Construction360.ViewModels
{
    public class RegisterViewModel
    {
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.Employee;
        public string? Error { get; set; }
    }
}
