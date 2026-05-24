using Construction360.Enums;
using Construction360.Models;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Construction360.Controllers
{
    public class EmployeeController : Controller
    {
        private int CurrentUserId => int.Parse(User.FindFirst("UserId")?.Value ?? "3");

        public IActionResult Dashboard()
        {
            var myAttendance = MockData.Attendance.Where(a => a.EmployeeId == CurrentUserId).ToList();
            var today = myAttendance.FirstOrDefault(a => a.Date.Date == DateTime.Today);
            var vm = new EmployeeDashboardViewModel
            {
                EmployeeName = User.Identity?.Name?.Split(' ').FirstOrDefault() ?? "User",
                DaysPresent = myAttendance.Count(a => a.Status == AttendanceStatus.Present && a.Date.Month == DateTime.Today.Month),
                MyEfficiency = 95,
                PendingLeaves = MockData.LeaveRequests.Count(l => l.EmployeeId == CurrentUserId && l.Status == LeaveStatus.Pending),
                LeaveBalance = 12,
                TodayAttendance = today,
                TasksCompleted = 15,
                TotalTasks = 18
            };
            return View(vm);
        }

        public IActionResult MyQRCode()
        {
            var user = MockData.Users.FirstOrDefault(u => u.Id == CurrentUserId);
            ViewBag.EmployeeId = user?.EmployeeId ?? "EMP-2023-001";
            ViewBag.FullName = user?.FullName ?? "Michael Chen";
            ViewBag.Initials = user?.Initials ?? "MC";
            return View();
        }

        public IActionResult Attendance()
        {
            var records = MockData.Attendance
                .Where(a => a.EmployeeId == CurrentUserId)
                .OrderByDescending(a => a.Date)
                .ToList();
            ViewBag.Present = records.Count(a => a.Status == AttendanceStatus.Present);
            ViewBag.Absent = records.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.Late = records.Count(a => a.Status == AttendanceStatus.Late);
            var checkIns = records.Where(a => a.CheckIn.HasValue).Select(a => a.CheckIn!.Value);
            ViewBag.AvgCheckIn = checkIns.Any()
                ? TimeSpan.FromMinutes(checkIns.Average(t => t.TotalMinutes))
                : (TimeSpan?)null;
            return View(records);
        }

        public IActionResult Productivity()
        {
            ViewBag.Efficiency = 95;
            ViewBag.TasksCompleted = 15;
            ViewBag.TasksTotal = 18;
            ViewBag.HoursWorked = 8;
            ViewBag.WeeklyAvg = 93;
            ViewBag.Trend = MockData.ProductivityTrend;
            return View();
        }

        public IActionResult LeaveRequests()
        {
            var requests = MockData.LeaveRequests.Where(l => l.EmployeeId == CurrentUserId).ToList();
            ViewBag.AnnualUsed = 12;
            ViewBag.AnnualTotal = 21;
            ViewBag.SickUsed = 8;
            ViewBag.SickTotal = 10;
            ViewBag.PersonalUsed = 4;
            ViewBag.PersonalTotal = 5;
            return View(requests);
        }

        [HttpPost]
        public IActionResult SubmitLeaveRequest(LeaveType type, DateTime startDate, DateTime endDate, string reason)
        {
            var user = MockData.Users.FirstOrDefault(u => u.Id == CurrentUserId);
            MockData.LeaveRequests.Add(new LeaveRequest
            {
                Id = MockData.LeaveRequests.Count + 1,
                EmployeeId = CurrentUserId,
                EmployeeName = user?.FullName ?? "Employee",
                Initials = user?.Initials ?? "EE",
                Type = type,
                Status = LeaveStatus.Pending,
                StartDate = startDate,
                EndDate = endDate,
                Reason = reason,
                SubmittedDate = DateTime.Today
            });
            return RedirectToAction("LeaveRequests");
        }

        public IActionResult Notifications()
        {
            return View(MockData.Notifications);
        }
    }
}
