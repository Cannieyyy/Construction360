using Construction360.Enums;

namespace Construction360.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public UserRole Role { get; set; }
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string Initials => string.Concat(FullName.Split(' ').Select(w => w.Length > 0 ? w[0].ToString() : ""));
        public bool IsActive { get; set; } = true;
    }
}
