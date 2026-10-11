using Construction360.Enums;
using Construction360.Models;
using Construction360.Repositories;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Construction360.Services;

namespace Construction360.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly IUserRepository _userRepository;
        private readonly DatabaseService _databaseService;

        public AdminController(
            IUserRepository userRepository,
            DatabaseService databaseService)
        {
            _userRepository = userRepository;
            _databaseService = databaseService;
        }

        // ===== DASHBOARD =====
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _userRepository.GetUserStatsAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalAccounts = stats.GetValueOrDefault("Total", 0),
                ActiveAccounts = stats.GetValueOrDefault("Active", 0),
                PendingAccounts = stats.GetValueOrDefault("Pending", 0),
                InactiveAccounts = stats.GetValueOrDefault("Inactive", 0),
                BuildingOccupancy = stats.GetValueOrDefault("Active", 0),
                EmployeeCountInside = 0,
                SupervisorCountInside = 0,
                SystemStatus = "Operational"
            };

            return View(vm);
        }

        // ===== LOGIN TRACKER =====
        public async Task<IActionResult> LoginTracker(string? search)
        {
            List<LoginRecord> logins;

            if (!string.IsNullOrWhiteSpace(search))
            {
                logins = await _userRepository.SearchLoginsAsync(search);
            }
            else
            {
                logins = await _userRepository.GetRecentLoginsAsync(100);
            }

            var vm = new LoginTrackerViewModel
            {
                TotalLogins = await _userRepository.GetTotalLoginsAsync(),
                TodayLogins = await _userRepository.GetTodayLoginsAsync(),
                RecentLogins = await _userRepository.GetRecentLoginsCountAsync(7),
                UniqueLocations = await _userRepository.GetUniqueLocationsCountAsync(),
                Logins = logins.Select(l => new LoginTrackerItemViewModel
                {
                    Name = l.Name,
                    Surname = l.Surname,
                    Initials = l.Initials,
                    LoginTime = l.LoginTime,
                    Location = l.Location
                }).ToList()
            };

            ViewBag.Search = search;
            return View(vm);
        }

        // ===== USER MANAGEMENT =====
        public async Task<IActionResult> Employees(string? search, string? status)
        {
            var allUsers = await _userRepository.GetAllUsersAsync();
            var usersList = allUsers.ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                usersList = usersList.Where(u =>
                    u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (u.EmployeeId ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                usersList = usersList
                    .Where(u => string.Equals(
                        u.AccountStatus,
                        status,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var stats = await _userRepository.GetUserStatsAsync();

            var vm = new UserManagementViewModel
            {
                TotalAccounts = stats.GetValueOrDefault("Total", 0),
                ActiveAccounts = stats.GetValueOrDefault("Active", 0),
                PendingAccounts = stats.GetValueOrDefault("Pending", 0),
                InactiveAccounts = stats.GetValueOrDefault("Inactive", 0),
                Users = usersList,
                Search = search,
                StatusFilter = status
            };

            return View(vm);
        }

        // ===== APPROVE USER =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            try
            {
                var result = await _userRepository.ApproveUserAsync(id);

                if (result)
                    TempData["SuccessMessage"] = "User approved successfully!";
                else
                    TempData["ErrorMessage"] = "Failed to approve user.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ApproveUser error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while approving the user.";
            }

            return RedirectToAction("Employees");
        }

        // ===== REJECT USER =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id)
        {
            try
            {
                var result = await _userRepository.RejectUserAsync(id);

                if (result)
                    TempData["SuccessMessage"] = "User rejected successfully!";
                else
                    TempData["ErrorMessage"] = "Failed to reject user.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RejectUser error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while rejecting the user.";
            }

            return RedirectToAction("Employees");
        }

        // ===== DEACTIVATE USER =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var result = await _userRepository.DeactivateUserAsync(id);

                if (result)
                    TempData["SuccessMessage"] = "User deactivated.";
                else
                    TempData["ErrorMessage"] = "Failed to deactivate user.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DeactivateUser error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred.";
            }

            return RedirectToAction("Employees");
        }

        // ===== ACTIVATE USER =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var result = await _userRepository.ActivateUserAsync(id);

                if (result)
                    TempData["SuccessMessage"] = "User activated successfully!";
                else
                    TempData["ErrorMessage"] = "Failed to activate user.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ActivateUser error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred.";
            }

            return RedirectToAction("Employees");
        }

        // ===== NOTIFICATIONS =====
        public IActionResult Notifications(string? filter)
        {
            var notifications = MockData.Notifications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter) && filter != "All")
            {
                if (filter == "Unread")
                    notifications = notifications.Where(n => !n.IsRead);
                else
                    notifications = notifications.Where(n => n.Type == filter);
            }

            ViewBag.UnreadCount = MockData.Notifications.Count(n => !n.IsRead);
            ViewBag.Filter = filter ?? "All";

            return View(notifications.ToList());
        }

        [HttpPost]
        public IActionResult MarkNotificationRead(int id)
        {
            var notification = MockData.Notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null) notification.IsRead = true;
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public IActionResult DeleteNotification(int id)
        {
            var notification = MockData.Notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null) MockData.Notifications.Remove(notification);
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public IActionResult MarkAllRead()
        {
            MockData.Notifications.ForEach(n => n.IsRead = true);
            return RedirectToAction("Notifications");
        }

        // ===== ANNOUNCEMENTS =====
        public IActionResult Announcements()
        {
            var announcements = MockData.Announcements
                .OrderByDescending(a => a.SentDate)
                .ToList();
            return View(announcements);
        }

        [HttpPost]
        public IActionResult SendAnnouncement(string title, string message, string audience)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                return RedirectToAction("Announcements");

            var adminName = User.Identity?.Name ?? "Administrator";

            MockData.Announcements.Insert(0, new Announcement
            {
                Id = MockData.Announcements.Count + 1,
                Title = title.Trim(),
                Message = message.Trim(),
                Audience = audience,
                SentBy = adminName,
                SentDate = DateTime.Now,
                Status = "Sent"
            });

            return RedirectToAction("Announcements");
        }

        // ============================================================
        // SEND NOTIFICATION PAGE
        // ============================================================

        [HttpGet]
        public IActionResult SendNotification()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(
     string title,
     string message,
     string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) ||
                    string.IsNullOrWhiteSpace(message))
                {
                    TempData["ErrorMessage"] =
                        "Please enter a notification title and message.";

                    return RedirectToAction("SendNotification");
                }

                string sql = @"
            INSERT INTO Notifications
            (
                User_ID,
                Title,
                Message,
                Type,
                DateSent,
                IsRead
            )
            SELECT
                User_ID,
                @Title,
                @Message,
                @Type,
                GETDATE(),
                0
            FROM Users
            WHERE IsActive = 1
              AND AccountStatus = 'Active';
        ";

                var parameters = new[]
                {
            new System.Data.SqlClient.SqlParameter("@Title", title.Trim()),
            new System.Data.SqlClient.SqlParameter("@Message", message.Trim()),
            new System.Data.SqlClient.SqlParameter("@Type", type ?? "Info")
        };

                var result = await Task.Run(() =>
                    _databaseService.ExecuteNonQuery(sql, parameters));

                if (result > 0)
                {
                    TempData["SuccessMessage"] =
                        "Notification sent successfully to all active users.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "The notification could not be sent because there are no active users.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"SendNotification error: {ex.Message}");

                TempData["ErrorMessage"] =
                    "An error occurred while sending the notification.";
            }

            return RedirectToAction("Notifications");
        }
    }
}