using System.Data.SqlClient;
using Construction360.Models;
using Construction360.Enums;
using Construction360.Services;

namespace Construction360.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseService _databaseService;

        public UserRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // ============================================================
        // ===== BASIC USER OPERATIONS =====
        // ============================================================

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
                ORDER BY CreatedDate DESC";

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
            if (user == null)
                return null;

            string sql = "SELECT Salt, PasswordHash FROM Users WHERE Email = @Email";
            var parameters = new[] { new SqlParameter("@Email", email) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            if (await reader.ReadAsync())
            {
                string salt = reader["Salt"].ToString();
                string storedHash = reader["PasswordHash"].ToString();

                if (PasswordHelper.VerifyPassword(password, salt, storedHash))
                {
                    // Update last login time
                    await UpdateLastLoginAsync(user.Id);

                    // Record the login if user is active
                    if (user.IsActive)
                    {
                        await RecordLoginAsync(user);
                    }

                    return user;
                }
            }
            return null;
        }

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            try
            {
                Console.WriteLine($"========== CreateUserAsync ==========");
                Console.WriteLine($"Email: {user.Email}, Username: {user.Username}, Role: {user.Role}");

                var salt = PasswordHelper.GenerateSalt();
                var hashedPassword = PasswordHelper.HashPassword(password, salt);

                if (string.IsNullOrEmpty(user.EmployeeId))
                {
                    var count = await GetUserCountAsync();
                    var year = DateTime.Now.Year;
                    user.EmployeeId = $"EMP-{year}-{(count + 1):D3}";
                }

                // Force IsActive to false for new users (admin must approve)
                user.IsActive = false;

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
                    new SqlParameter("@Department", (object)user.Department ?? DBNull.Value),
                    new SqlParameter("@Position", (object)user.Position ?? DBNull.Value),
                    new SqlParameter("@IsActive", user.IsActive),
                    new SqlParameter("@CreatedDate", DateTime.Now)
                };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                Console.WriteLine($"✅ Rows affected: {result}");
                return result > 0;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"❌ SQL ERROR: {sqlEx.Message} (Code: {sqlEx.Number})");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                return false;
            }
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
                new SqlParameter("@Department", (object)user.Department ?? DBNull.Value),
                new SqlParameter("@Position", (object)user.Position ?? DBNull.Value),
                new SqlParameter("@IsActive", user.IsActive)
            };

            var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
            return result > 0;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
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

        // ============================================================
        // ===== USER APPROVAL / ACTIVATION =====
        // ============================================================

        public async Task<bool> ApproveUserAsync(int userId)
        {
            try
            {
                Console.WriteLine($"========== ApproveUserAsync ==========");
                Console.WriteLine($"User ID: {userId}");

                string sql = "UPDATE Users SET IsActive = 1 WHERE User_ID = @UserId";
                var parameters = new[] { new SqlParameter("@UserId", userId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));

                Console.WriteLine($"✅ Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ApproveUserAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RejectUserAsync(int userId)
        {
            try
            {
                Console.WriteLine($"========== RejectUserAsync ==========");
                Console.WriteLine($"User ID: {userId}");

                // Delete the user completely (they can re-register if rejected by mistake)
                string sql = "DELETE FROM Users WHERE User_ID = @UserId AND IsActive = 0";
                var parameters = new[] { new SqlParameter("@UserId", userId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));

                Console.WriteLine($"✅ Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RejectUserAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                string sql = "UPDATE Users SET IsActive = 0 WHERE User_ID = @UserId";
                var parameters = new[] { new SqlParameter("@UserId", userId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DeactivateUserAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<User>> GetPendingUsersAsync()
        {
            return await GetUsersByStatusAsync("pending");
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await GetUsersByStatusAsync("active");
        }

        public async Task<List<User>> GetUsersByStatusAsync(string status)
        {
            var users = new List<User>();

            try
            {
                string sql = @"
                    SELECT User_ID, FullName, Username, Email, PasswordHash, Salt, 
                           Role, EmployeeId, Department, Position, IsActive, CreatedDate, LastLoginDate
                    FROM Users";

                switch (status?.ToLower())
                {
                    case "pending":
                        sql += " WHERE IsActive = 0";
                        break;
                    case "active":
                        sql += " WHERE IsActive = 1";
                        break;
                    case "inactive":
                        sql += " WHERE IsActive = 0";
                        break;
                }

                sql += " ORDER BY CreatedDate DESC";

                using var reader = _databaseService.ExecuteReader(sql);
                while (await reader.ReadAsync())
                {
                    users.Add(MapUser(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetUsersByStatusAsync error: {ex.Message}");
            }

            return users;
        }

        public async Task<Dictionary<string, int>> GetUserStatsAsync()
        {
            var stats = new Dictionary<string, int>
            {
                { "Total", 0 },
                { "Active", 0 },
                { "Pending", 0 },
                { "Inactive", 0 }
            };

            try
            {
                string sql = @"
                    SELECT 
                        COUNT(*) AS Total,
                        SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS Active,
                        SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS Pending
                    FROM Users";

                using var reader = _databaseService.ExecuteReader(sql);
                if (await reader.ReadAsync())
                {
                    stats["Total"] = Convert.ToInt32(reader["Total"]);
                    stats["Active"] = Convert.ToInt32(reader["Active"]);
                    stats["Pending"] = Convert.ToInt32(reader["Pending"]);
                    stats["Inactive"] = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetUserStatsAsync error: {ex.Message}");
            }

            return stats;
        }

        // ============================================================
        // ===== LOGIN TRACKING =====
        // ============================================================

        public async Task RecordLoginAsync(User user, string location = "Main Office")
        {
            try
            {
                var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.FirstOrDefault() ?? user.FullName;
                var surname = nameParts.Length > 1
                    ? string.Join(" ", nameParts.Skip(1))
                    : "";
                var initials = string.Concat(
                    nameParts.Select(x => x.Length > 0 ? x[0].ToString() : "")
                ).ToUpper();

                string sql = @"
                    INSERT INTO LoginRecords (User_ID, Name, Surname, Initials, LoginTime, Location)
                    VALUES (@UserId, @Name, @Surname, @Initials, @LoginTime, @Location)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", user.Id),
                    new SqlParameter("@Name", firstName),
                    new SqlParameter("@Surname", surname),
                    new SqlParameter("@Initials", initials),
                    new SqlParameter("@LoginTime", DateTime.Now),
                    new SqlParameter("@Location", location)
                };

                await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                Console.WriteLine($"✅ Login recorded for {user.Email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RecordLoginAsync error: {ex.Message}");
                // Don't throw — login should succeed even if tracking fails
            }
        }

        public async Task<List<LoginRecord>> GetRecentLoginsAsync(int count)
        {
            var records = new List<LoginRecord>();

            try
            {
                string sql = @"
                    SELECT TOP (@Count)
                        LoginRecord_ID, User_ID, Name, Surname, Initials, LoginTime, Location
                    FROM LoginRecords
                    ORDER BY LoginTime DESC";

                var parameters = new[] { new SqlParameter("@Count", count) };

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    records.Add(MapLoginRecord(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetRecentLoginsAsync error: {ex.Message}");
            }

            return records;
        }

        public async Task<List<LoginRecord>> SearchLoginsAsync(string searchTerm)
        {
            var records = new List<LoginRecord>();

            try
            {
                string sql = @"
                    SELECT 
                        LoginRecord_ID, User_ID, Name, Surname, Initials, LoginTime, Location
                    FROM LoginRecords
                    WHERE Name LIKE @Search 
                       OR Surname LIKE @Search
                       OR (Name + ' ' + Surname) LIKE @Search
                    ORDER BY LoginTime DESC";

                var parameters = new[]
                {
                    new SqlParameter("@Search", $"%{searchTerm}%")
                };

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    records.Add(MapLoginRecord(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SearchLoginsAsync error: {ex.Message}");
            }

            return records;
        }

        public async Task<int> GetTotalLoginsAsync()
        {
            try
            {
                string sql = "SELECT COUNT(*) FROM LoginRecords";
                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql));
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTotalLoginsAsync error: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetTodayLoginsAsync()
        {
            try
            {
                string sql = @"
                    SELECT COUNT(*) FROM LoginRecords 
                    WHERE CAST(LoginTime AS DATE) = CAST(GETDATE() AS DATE)";

                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql));
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTodayLoginsAsync error: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetRecentLoginsCountAsync(int days)
        {
            try
            {
                string sql = @"
                    SELECT COUNT(*) FROM LoginRecords 
                    WHERE LoginTime >= DATEADD(day, -@Days, GETDATE())";

                var parameters = new[] { new SqlParameter("@Days", days) };
                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql, parameters));
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetRecentLoginsCountAsync error: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetUniqueLocationsCountAsync()
        {
            try
            {
                string sql = "SELECT COUNT(DISTINCT Location) FROM LoginRecords WHERE Location IS NOT NULL";
                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql));
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetUniqueLocationsCountAsync error: {ex.Message}");
                return 0;
            }
        }

        // ============================================================
        // ===== PRIVATE HELPERS =====
        // ============================================================

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
                FullName = reader["FullName"].ToString() ?? "",
                Username = reader["Username"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? "",
                Role = Enum.Parse<UserRole>(reader["Role"].ToString() ?? "Employee"),
                EmployeeId = reader["EmployeeId"]?.ToString() ?? "",
                Department = reader["Department"]?.ToString() ?? "",
                Position = reader["Position"]?.ToString() ?? "",
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedDate = reader["CreatedDate"] != DBNull.Value
                    ? Convert.ToDateTime(reader["CreatedDate"])
                    : DateTime.MinValue,
                LastLoginDate = reader["LastLoginDate"] != DBNull.Value
                    ? Convert.ToDateTime(reader["LastLoginDate"])
                    : (DateTime?)null
            };
        }

        private LoginRecord MapLoginRecord(SqlDataReader reader)
        {
            return new LoginRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("LoginRecord_ID")),
                UserId = reader.GetInt32(reader.GetOrdinal("User_ID")),
                Name = reader["Name"]?.ToString() ?? "",
                Surname = reader["Surname"]?.ToString() ?? "",
                Initials = reader["Initials"]?.ToString() ?? "",
                LoginTime = reader["LoginTime"] != DBNull.Value
                    ? Convert.ToDateTime(reader["LoginTime"])
                    : DateTime.MinValue,
                Location = reader["Location"]?.ToString() ?? ""
            };
        }
    }
}