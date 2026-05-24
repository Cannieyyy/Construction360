using Construction360.Enums;
using Construction360.Models;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Construction360.Controllers
{
    public class SupervisorController : Controller
    {
        public IActionResult Dashboard()
        {
            var vm = new SupervisorDashboardViewModel
            {
                TeamSize = 50,
                PresentToday = 42,
                PendingApprovals = MockData.LeaveRequests.Count(l => l.Status == LeaveStatus.Pending),
                TeamProductivity = 94,
                WeeklyAttendance = MockData.WeeklyAttendanceData,
                ProductivityTrend = MockData.ProductivityTrend
            };
            return View(vm);
        }

        public IActionResult QRScanner() => View();

        public IActionResult Attendance()
        {
            var today = MockData.Attendance.Where(a => a.Date.Date == DateTime.Today).ToList();
            ViewBag.Present = today.Count(a => a.Status == AttendanceStatus.Present);
            ViewBag.Absent = today.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.Late = today.Count(a => a.Status == AttendanceStatus.Late);
            ViewBag.OnLeave = today.Count(a => a.Status == AttendanceStatus.OnLeave);
            ViewBag.Weekly = MockData.WeeklyAttendanceData;
            return View(today);
        }

        public IActionResult Productivity()
        {
            ViewBag.AvgEfficiency = 97;
            ViewBag.TasksCompleted = 72;
            ViewBag.TotalHours = 31.5;
            ViewBag.TopPerformer = "Emily";
            ViewBag.Trend = MockData.ProductivityTrend;
            return View();
        }

        public IActionResult LeaveApproval(string? filter)
        {
            var leaves = MockData.LeaveRequests.AsQueryable();
            if (!string.IsNullOrEmpty(filter) && filter != "All")
                leaves = leaves.Where(l => l.Status.ToString() == filter);
            ViewBag.Filter = filter ?? "Pending";
            ViewBag.PendingCount = MockData.LeaveRequests.Count(l => l.Status == LeaveStatus.Pending);
            return View(leaves.ToList());
        }

        [HttpPost]
        public IActionResult ApproveLeave(int id)
        {
            MockData.ApproveLeave(id);
            return RedirectToAction("LeaveApproval");
        }

        [HttpPost]
        public IActionResult RejectLeave(int id)
        {
            MockData.RejectLeave(id);
            return RedirectToAction("LeaveApproval");
        }

        public IActionResult Notifications()
        {
            ViewBag.UnreadCount = MockData.Notifications.Count(n => !n.IsRead);
            return View(MockData.Notifications);
        }
    }
}
