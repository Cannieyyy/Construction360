using Construction360.Models;

namespace Construction360.ViewModels
{
    public class UserManagementViewModel
    {
        // Stats
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int PendingAccounts { get; set; }
        public int InactiveAccounts { get; set; }

        // Users list
        public List<User> Users { get; set; } = new();

        // Filter info
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
    }
}