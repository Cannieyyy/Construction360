using Construction360.Models;
namespace Construction360.ViewModels
{
    public class UserManagementViewModel
    {
        public int TotalAccounts { get; set; }
        public int PendingAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int InactiveAccounts { get; set; }

        public List<UserManagementItemViewModel> Users { get; set; }
            = new List<UserManagementItemViewModel>();
    }
}
