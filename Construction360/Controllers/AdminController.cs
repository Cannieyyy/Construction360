using Construction360.Enums;
using Construction360.Models;
using Construction360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction360.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalEmployees = 50,
                PresentToday = 42,
                AbsentToday = 3,
                PendingLeaves = MockData.LeaveRequests.Count(l => l.Status == LeaveStatus.Pending),
                OnLeave = 5,
                LateArrivals = 2,
                AvgProductivity = 94,
                WeeklyAttendance = MockData.WeeklyAttendanceData,
                DepartmentDistribution = new Dictionary<string, double>
            {
                { "Manufacturing", 44 },
                { "Quality Control", 15 },
                { "Administration", 9 },
                { "Maintenance", 10 },
                { "Logistics", 22 }
            }
            };
            return View(vm);
        }

        public IActionResult Employees(string? search, string? department, string? status)
        {
            var employees = MockData.Employees.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                employees = employees.Where(e => e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || e.EmployeeId.Contains(search, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(department) && department != "All")
                employees = employees.Where(e => e.Department == department);
            if (!string.IsNullOrEmpty(status) && status != "All")
                employees = employees.Where(e => e.Status == status);

            ViewBag.Search = search;
            ViewBag.Department = department;
            ViewBag.Status = status;
            return View(employees.ToList());
        }

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

        public IActionResult LeaveManagement(string? filter)
        {
            var leaves = MockData.LeaveRequests.AsQueryable();
            if (!string.IsNullOrEmpty(filter) && filter != "All")
                leaves = leaves.Where(l => l.Status.ToString() == filter);
            ViewBag.Filter = filter ?? "All";
            ViewBag.PendingCount = MockData.LeaveRequests.Count(l => l.Status == LeaveStatus.Pending);
            return View(leaves.ToList());
        }

        [HttpPost]
        public IActionResult ApproveLeave(int id)
        {
            MockData.ApproveLeave(id);
            return RedirectToAction("LeaveManagement");
        }

        [HttpPost]
        public IActionResult RejectLeave(int id)
        {
            MockData.RejectLeave(id);
            return RedirectToAction("LeaveManagement");
        }

        public IActionResult Notifications()
        {
            ViewBag.UnreadCount = MockData.Notifications.Count(n => !n.IsRead);
            return View(MockData.Notifications);
        }

        [HttpPost]
        public IActionResult MarkAllRead()
        {
            MockData.Notifications.ForEach(n => n.IsRead = true);
            return RedirectToAction("Notifications");
        }
    }
}
