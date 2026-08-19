using Construction360.Models;
using Construction360.Enums;
namespace Construction360.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAzync(int id);
        Task<User> GetUserByEmailAzync(string email);
        Task<User> GetUserByUsernameAzync(string username);
        Task<IEnumerable<User>> GetAllUsersAzync();
        Task<User> AuthenticateAsync(string email, string password);
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UserExistsAsync(string email, string username);
        Task UpdateLastLoginAsync(int userId);
    }
}
