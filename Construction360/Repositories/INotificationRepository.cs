using Construction360.Models;

namespace Construction360.Repositories
{
    public interface INotificationRepository
    {
        // Get notifications for a specific user (by role and/or user id)
        Task<List<Notification>> GetNotificationsForUserAsync(int userId, string role);

        // Get filtered notifications
        Task<List<Notification>> GetFilteredNotificationsAsync(int userId, string role, string? filter);

        // Get unread count
        Task<int> GetUnreadCountAsync(int userId, string role);

        // Mark single notification as read
        Task<bool> MarkAsReadAsync(int notificationId);

        // Mark all as read for a user
        Task<bool> MarkAllAsReadAsync(int userId, string role);

        // Delete a notification
        Task<bool> DeleteAsync(int notificationId);

        // Create a new notification (used by Admin)
        Task<bool> CreateAsync(Notification notification, int? targetUserId);
    }
}