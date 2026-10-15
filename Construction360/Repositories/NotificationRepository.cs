using System.Data.SqlClient;
using Construction360.Models;
using Construction360.Services;

namespace Construction360.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly DatabaseService _databaseService;

        public NotificationRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // ============================================================
        // ===== GET NOTIFICATIONS FOR A USER =====
        // ============================================================

        public async Task<List<Notification>> GetNotificationsForUserAsync(int userId, string role)
        {
            return await GetFilteredNotificationsAsync(userId, role, null);
        }

        public async Task<List<Notification>> GetFilteredNotificationsAsync(int userId, string role, string? filter)
        {
            var notifications = new List<Notification>();

            try
            {
                // A notification is visible to a user if:
                // 1. It was sent directly to them (User_ID = userId)
                // 2. It was broadcast to their role (Audience = role)
                // 3. It was broadcast to Everyone (Audience = 'Everyone')
                string sql = @"
                    SELECT 
                        Notification_ID,
                        Title,
                        Message,
                        Type,
                        DateSent,
                        IsRead,
                        User_ID,
                        Audience
                    FROM Notifications
                    WHERE User_ID = @UserId 
                       OR Audience = @Role 
                       OR Audience = 'Everyone'
                       OR Audience IS NULL";

                // Apply filter
                if (!string.IsNullOrWhiteSpace(filter) && filter != "All")
                {
                    if (filter == "Unread")
                    {
                        sql += " AND IsRead = 0";
                    }
                    else
                    {
                        sql += " AND Type = @Type";
                    }
                }

                sql += " ORDER BY DateSent DESC";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Role", role)
                };

                if (!string.IsNullOrWhiteSpace(filter) && filter != "All" && filter != "Unread")
                {
                    parameters.Add(new SqlParameter("@Type", filter));
                }

                using var reader = _databaseService.ExecuteReader(sql, parameters.ToArray());
                while (await reader.ReadAsync())
                {
                    notifications.Add(MapNotification(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetFilteredNotificationsAsync error: {ex.Message}");
            }

            return notifications;
        }

        // ============================================================
        // ===== UNREAD COUNT =====
        // ============================================================

        public async Task<int> GetUnreadCountAsync(int userId, string role)
        {
            try
            {
                string sql = @"
                    SELECT COUNT(*) 
                    FROM Notifications
                    WHERE IsRead = 0
                      AND (User_ID = @UserId 
                           OR Audience = @Role 
                           OR Audience = 'Everyone'
                           OR Audience IS NULL)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Role", role)
                };

                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql, parameters));
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetUnreadCountAsync error: {ex.Message}");
                return 0;
            }
        }

        // ============================================================
        // ===== MARK AS READ =====
        // ============================================================

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                string sql = "UPDATE Notifications SET IsRead = 1 WHERE Notification_ID = @Id";
                var parameters = new[] { new SqlParameter("@Id", notificationId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MarkAsReadAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId, string role)
        {
            try
            {
                string sql = @"
                    UPDATE Notifications
                    SET IsRead = 1
                    WHERE IsRead = 0
                      AND (User_ID = @UserId 
                           OR Audience = @Role 
                           OR Audience = 'Everyone'
                           OR Audience IS NULL)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Role", role)
                };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MarkAllAsReadAsync error: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // ===== DELETE =====
        // ============================================================

        public async Task<bool> DeleteAsync(int notificationId)
        {
            try
            {
                string sql = "DELETE FROM Notifications WHERE Notification_ID = @Id";
                var parameters = new[] { new SqlParameter("@Id", notificationId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DeleteAsync error: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // ===== CREATE =====
        // ============================================================

        public async Task<bool> CreateAsync(Notification notification, int? targetUserId)
        {
            try
            {
                string sql = @"
                    INSERT INTO Notifications (User_ID, Title, Message, Type, DateSent, IsRead, Audience)
                    VALUES (@UserId, @Title, @Message, @Type, @DateSent, 0, @Audience)";

                var parameters = new[]
                {
                    new SqlParameter("@UserId", (object?)targetUserId ?? DBNull.Value),
                    new SqlParameter("@Title", notification.Title),
                    new SqlParameter("@Message", notification.Message),
                    new SqlParameter("@Type", notification.Type ?? "Info"),
                    new SqlParameter("@DateSent", DateTime.Now),
                    new SqlParameter("@Audience", (object?)notification.Audience ?? "Everyone")
                };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CreateAsync error: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // ===== MAPPING =====
        // ============================================================

        private Notification MapNotification(SqlDataReader reader)
        {
            return new Notification
            {
                Id = reader.GetInt32(reader.GetOrdinal("Notification_ID")),
                Title = reader["Title"]?.ToString() ?? "",
                Message = reader["Message"]?.ToString() ?? "",
                Type = reader["Type"]?.ToString() ?? "Info",
                Date = reader["DateSent"] != DBNull.Value
                    ? Convert.ToDateTime(reader["DateSent"])
                    : DateTime.Now,
                IsRead = reader["IsRead"] != DBNull.Value && Convert.ToBoolean(reader["IsRead"]),
                UserId = reader["User_ID"] != DBNull.Value
                    ? Convert.ToInt32(reader["User_ID"])
                    : null,
                Audience = reader["Audience"]?.ToString()
            };
        }
    }
}