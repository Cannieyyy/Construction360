using Construction360.Enums;
using Construction360.Models;
using Construction360.Repositories;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction360.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ISupervisorRepository _supervisorRepository;

        public SupervisorController(ISupervisorRepository SuperRepo)
        {
            _supervisorRepository = SuperRepo;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var vm = await _supervisorRepository.GetDashboardDataAsync(supervisorId);
                ViewData["PageTitle"] = "Supervisor Dashboard";
                return View(vm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard error: {ex.Message}");
                ModelState.AddModelError("", "Unable to load dashboard data. Please try again.");
                return View(new SupervisorDashboardViewModel());
            }
        }

        public async Task<IActionResult> QRScanner()
        {
            return View();
        }

        public async Task<IActionResult> Attendance()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var today = await _supervisorRepository.GetTeamAttendanceAsync(supervisorId);
                var stats = await _supervisorRepository.GetAttendanceStatsAsync(supervisorId, DateTime.Today);
                var weekly = await GetWeeklyAttendanceAsync(supervisorId);

                ViewBag.Present = stats.GetValueOrDefault("Present", 0);
                ViewBag.Absent = stats.GetValueOrDefault("Absent", 0);
                ViewBag.Late = stats.GetValueOrDefault("Late", 0);
                ViewBag.OnLeave = stats.GetValueOrDefault("OnLeave", 0);
                ViewBag.Weekly = weekly;

                return View(today);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attendance error: {ex.Message}");
                ViewBag.Present = 0;
                ViewBag.Absent = 0;
                ViewBag.Late = 0;
                ViewBag.OnLeave = 0;
                ViewBag.Weekly = new List<WeeklyAttendance>();
                return View(new List<AttendanceRecord>());
            }
        }

        public async Task<IActionResult> Productivity()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Get ALL data from repository - NO RANDOM DATA
                var trend = await _supervisorRepository.GetTeamProductivityAsync(supervisorId);
                var team = await _supervisorRepository.GetTeamMembersAsync(supervisorId);
                var tasksCompleted = await _supervisorRepository.GetTotalTasksCompletedAsync(supervisorId);
                var totalHours = await _supervisorRepository.GetTotalHoursWorkedAsync(supervisorId);
                var topPerformer = await _supervisorRepository.GetTopPerformerAsync(supervisorId);

                // Calculate average efficiency from actual data (only if data exists)
                double avgEfficiency = 0;
                if (trend != null && trend.Any() && trend.Any(p => p.Actual > 0))
                {
                    avgEfficiency = Math.Round(trend.Where(p => p.Actual > 0).Average(p => p.Actual), 1);
                }

                // Pass data to view - ALL FROM DATABASE, NO RANDOM
                ViewBag.Trend = trend ?? new List<ProductivityRecord>();
                ViewBag.TeamSize = team?.Count ?? 0;
                ViewBag.AvgEfficiency = avgEfficiency;
                ViewBag.TasksCompleted = tasksCompleted;  // From database
                ViewBag.TotalHours = totalHours;          // From database
                ViewBag.TopPerformer = topPerformer;      // From database
                ViewBag.HasData = trend != null && trend.Any(p => p.Actual > 0);

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Productivity page error: {ex.Message}");

                // Return ALL ZEROS - NO RANDOM DATA
                ViewBag.Trend = new List<ProductivityRecord>
                {
                    new() { Week = "Week 1", Target = 90, Actual = 0 },
                    new() { Week = "Week 2", Target = 90, Actual = 0 },
                    new() { Week = "Week 3", Target = 90, Actual = 0 },
                    new() { Week = "Week 4", Target = 90, Actual = 0 }
                };
                ViewBag.TeamSize = 0;
                ViewBag.AvgEfficiency = 0;
                ViewBag.TasksCompleted = 0;
                ViewBag.TotalHours = 0;
                ViewBag.TopPerformer = "N/A";
                ViewBag.HasData = false;

                ModelState.AddModelError("", "Unable to load productivity data. Please try again.");
                return View();
            }
        }

        public async Task<IActionResult> LeaveApproval(string? filter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var status = string.IsNullOrEmpty(filter) || filter == "All" ? null : filter;
                var leaves = await _supervisorRepository.GetTeamLeaveRequestsAsync(supervisorId, status);
                var pendingCount = (await _supervisorRepository.GetTeamLeaveRequestsAsync(supervisorId, "Pending")).Count;

                ViewBag.Filter = filter ?? "Pending";
                ViewBag.PendingCount = pendingCount;

                return View(leaves);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LeaveApproval error: {ex.Message}");
                ViewBag.Filter = filter ?? "Pending";
                ViewBag.PendingCount = 0;
                return View(new List<LeaveRequest>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            try
            {
                var result = await _supervisorRepository.ApproveLeaveAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Leave request approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve leave request.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApproveLeave error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while processing your request.";
            }

            return RedirectToAction("LeaveApproval");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLeave(int id)
        {
            try
            {
                var result = await _supervisorRepository.RejectLeaveAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Leave request rejected successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject leave request.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RejectLeave error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while processing your request.";
            }

            return RedirectToAction("LeaveApproval");
        }

        public async Task<IActionResult> Notifications()
        {
            ViewBag.UnreadCount = 0;
            return View(new List<Notification>());
        }

        // ===== PRIVATE HELPER METHODS - ALL FROM DATABASE =====

        private async Task<List<WeeklyAttendance>> GetWeeklyAttendanceAsync(int supervisorId)
        {
            try
            {
                var weekly = new List<WeeklyAttendance>();
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);

                for (int i = 0; i < 7; i++)
                {
                    var date = startOfWeek.AddDays(i);
                    var stats = await _supervisorRepository.GetAttendanceStatsAsync(supervisorId, date);

                    weekly.Add(new WeeklyAttendance
                    {
                        Day = days[i],
                        Present = stats.GetValueOrDefault("Present", 0),
                        Absent = stats.GetValueOrDefault("Absent", 0),
                        Late = stats.GetValueOrDefault("Late", 0)
                    });
                }

                return weekly;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetWeeklyAttendanceAsync error: {ex.Message}");
                return new List<WeeklyAttendance>();
            }
        }
    }
}