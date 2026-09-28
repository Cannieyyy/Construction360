using Construction360.Models;
using Construction360.Enums;

namespace Construction360.Repositories
{
    public interface IUserRepository
    {
        // ===== BASIC USER OPERATIONS =====
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> AuthenticateAsync(string email, string password);
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UserExistsAsync(string email, string username);
        Task UpdateLastLoginAsync(int userId);

        // ===== USER APPROVAL / ACTIVATION =====
        Task<bool> ApproveUserAsync(int userId);
        Task<bool> RejectUserAsync(int userId);
        Task<bool> DeactivateUserAsync(int userId);
        Task<List<User>> GetPendingUsersAsync();
        Task<List<User>> GetActiveUsersAsync();
        Task<List<User>> GetUsersByStatusAsync(string status);
        Task<Dictionary<string, int>> GetUserStatsAsync();

        // ===== LOGIN TRACKING =====
        Task<List<LoginRecord>> GetRecentLoginsAsync(int count);
        Task<List<LoginRecord>> SearchLoginsAsync(string searchTerm);
        Task<int> GetTotalLoginsAsync();
        Task<int> GetTodayLoginsAsync();
        Task<int> GetRecentLoginsCountAsync(int days);
        Task<int> GetUniqueLocationsCountAsync();
        Task RecordLoginAsync(User user, string location = "Main Office");
    }
}