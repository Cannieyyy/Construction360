using Construction360.Enums;
using System.ComponentModel.DataAnnotations;

namespace Construction360.ViewModels
{
    public class RegisterViewModel
    {
        //For the full name
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        //For the Username
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens")]
        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        //For the email
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email must be less than 100 characters")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        //For the password
        [Required(ErrorMessage = "Password is required")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        //Confirming the password
        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";

        //For the user role
        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "Role")]
        public UserRole Role { get; set; } = UserRole.Employee;

        //For the departments
        [Display(Name = "Department")]
        public string? Department { get; set; }

        //For the position
        [Display(Name = "Position")]
        public string? Position { get; set; }

        // For any errors
        public string? Error { get; set; }
    }
}