using System.Data.SqlClient;
using Construction360.Models;
using Construction360.Enums;
using Construction360.Services;

namespace Construction360.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseService _databaseService;

        public UserRepository()
        {
            _databaseService = new DatabaseService();
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            string sql = @"
                SELECT User_ID, FullName, Username, Email, PasswordHash, Salt, 
                       Role, EmployeeId, Department, Position, IsActive, CreatedDate, LastLoginDate
                FROM Users 
                WHERE User_ID = @UserId";

            var parameters = new[] { new SqlParameter("@UserId", id) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            string sql = @"
                SELECT User_ID, FullName, Username, Email, PasswordHash, Salt, 
                       Role, EmployeeId, Department, Position, IsActive, CreatedDate, LastLoginDate
                FROM Users 
                WHERE Email = @Email";

            var parameters = new[] { new SqlParameter("@Email", email) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            string sql = @"
                SELECT User_ID, FullName, Username, Email, PasswordHash, Salt, 
                       Role, EmployeeId, Department, Position, IsActive, CreatedDate, LastLoginDate
                FROM Users 
                WHERE Username = @Username";

            var parameters = new[] { new SqlParameter("@Username", username) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            string sql = @"
                SELECT User_ID, FullName, Username, Email, PasswordHash, Salt, 
                       Role, EmployeeId, Department, Position, IsActive, CreatedDate, LastLoginDate
                FROM Users 
                ORDER BY FullName";

            using var reader = _databaseService.ExecuteReader(sql);
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }
            return users;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !user.IsActive)
                return null;

            // Get salt and verify password
            string sql = "SELECT Salt, PasswordHash FROM Users WHERE Email = @Email";
            var parameters = new[] { new SqlParameter("@Email", email) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            if (await reader.ReadAsync())
            {
                string salt = reader["Salt"].ToString();
                string storedHash = reader["PasswordHash"].ToString();

                if (PasswordHelper.VerifyPassword(password, salt, storedHash))
                {
                    // Update last login
                    await UpdateLastLoginAsync(user.Id);
                    return user;
                }
            }
            return null;
        }

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            // Generate salt and hash password
            var salt = PasswordHelper.GenerateSalt();
            var hashedPassword = PasswordHelper.HashPassword(password, salt);

            // Generate Employee ID if not provided
            if (string.IsNullOrEmpty(user.EmployeeId))
            {
                var count = await GetUserCountAsync();
                var year = DateTime.Now.Year;
                user.EmployeeId = $"EMP-{year}-{(count + 1):D3}";
            }

            string sql = @"
                INSERT INTO Users (FullName, Username, Email, PasswordHash, Salt, Role, 
                                   EmployeeId, Department, Position, IsActive, CreatedDate)
                VALUES (@FullName, @Username, @Email, @PasswordHash, @Salt, @Role, 
                        @EmployeeId, @Department, @Position, @IsActive, @CreatedDate)";

            var parameters = new[]
            {
                new SqlParameter("@FullName", user.FullName),
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@PasswordHash", hashedPassword),
                new SqlParameter("@Salt", salt),
                new SqlParameter("@Role", user.Role.ToString()),
                new SqlParameter("@EmployeeId", user.EmployeeId),
                new SqlParameter("@Department", user.Department ?? (object)DBNull.Value),
                new SqlParameter("@Position", user.Position ?? (object)DBNull.Value),
                new SqlParameter("@IsActive", user.IsActive),
                new SqlParameter("@CreatedDate", DateTime.Now)
            };

            var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
            return result > 0;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            string sql = @"
                UPDATE Users 
                SET FullName = @FullName,
                    Username = @Username,
                    Email = @Email,
                    Role = @Role,
                    Department = @Department,
                    Position = @Position,
                    IsActive = @IsActive
                WHERE User_ID = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@UserId", user.Id),
                new SqlParameter("@FullName", user.FullName),
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@Role", user.Role.ToString()),
                new SqlParameter("@Department", user.Department ?? (object)DBNull.Value),
                new SqlParameter("@Position", user.Position ?? (object)DBNull.Value),
                new SqlParameter("@IsActive", user.IsActive)
            };

            var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
            return result > 0;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            // Soft delete - just deactivate
            string sql = "UPDATE Users SET IsActive = 0 WHERE User_ID = @UserId";
            var parameters = new[] { new SqlParameter("@UserId", id) };
            var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
            return result > 0;
        }

        public async Task<bool> UserExistsAsync(string email, string username)
        {
            string sql = "SELECT COUNT(*) FROM Users WHERE Email = @Email OR Username = @Username";
            var parameters = new[]
            {
                new SqlParameter("@Email", email),
                new SqlParameter("@Username", username)
            };
            var result = await Task.Run(() => _databaseService.ExecuteScalar(sql, parameters));
            return Convert.ToInt32(result) > 0;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            string sql = "UPDATE Users SET LastLoginDate = @LoginDate WHERE User_ID = @UserId";
            var parameters = new[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@LoginDate", DateTime.Now)
            };
            await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
        }

        private async Task<int> GetUserCountAsync()
        {
            string sql = "SELECT COUNT(*) FROM Users";
            var result = await Task.Run(() => _databaseService.ExecuteScalar(sql));
            return Convert.ToInt32(result);
        }

        private User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("User_ID")),
                FullName = reader["FullName"].ToString(),
                Username = reader["Username"].ToString(),
                Email = reader["Email"].ToString(),
                Role = Enum.Parse<UserRole>(reader["Role"].ToString()),
                EmployeeId = reader["EmployeeId"]?.ToString() ?? "",
                Department = reader["Department"]?.ToString() ?? "",
                Position = reader["Position"]?.ToString() ?? "",
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue,
                LastLoginDate = reader["LastLoginDate"] != DBNull.Value ? Convert.ToDateTime(reader["LastLoginDate"]) : (DateTime?)null
            };
        }

        public Task<User> GetUserByIdAzync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetUserByEmailAzync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetUserByUsernameAzync(string username)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> GetAllUsersAzync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> CreateUserAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CreateUserAsync(string email, string username)
        {
            throw new NotImplementedException();
        }
    }
}