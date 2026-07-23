using Construction360.Enums;
using Construction360.ViewModels;

namespace Construction360.Models
{
    public static class MockData
    {
        public static List<User> Users { get; } = new()
    {
        new User { Id = 1, FullName = "Keoagile Mafora", Username = "keo.admin", Email = "admin@rocla.com", PasswordHash = "admin123", Role = UserRole.Admin, Department = "Administration", Position = "Administrator", EmployeeId = "EMP-ADMIN-001" },
        new User { Id = 2, FullName = "Mulweli Mbedzi", Username = "mulweli.super", Email = "supervisor@rocla.com", PasswordHash = "super123", Role = UserRole.Supervisor, Department = "Manufacturing", Position = "Supervisor", EmployeeId = "EMP-SUP-001" },
    };

        public static List<Employee> Employees { get; } = new()
    {
        new Employee { Id = 1, FullName = "Ndivhuwo Ranwedzi", EmployeeId = "EMP-2023-001", Department = "Manufacturing", Position = "Machine Operator", Status = "Active" },
        new Employee { Id = 2, FullName = "Kopano Leshope", EmployeeId = "EMP-2023-002", Department = "Quality Control", Position = "QC Inspector", Status = "Active" },
        new Employee { Id = 3, FullName = "Karabo Dube", EmployeeId = "EMP-2023-003", Department = "Logistics", Position = "Forklift Operator", Status = "Active" },
        new Employee { Id = 4, FullName = "Kamogelo Mahube", EmployeeId = "EMP-2023-004", Department = "Manufacturing", Position = "Production Line Lead", Status = "Active" },
        new Employee { Id = 5, FullName = "John Seleka", EmployeeId = "EMP-2023-005", Department = "Maintenance", Position = "Equipment Technician", Status = "On leave" },
    };

        public static List<AttendanceRecord> Attendance { get; } = new()
    {
        new AttendanceRecord { Id = 1, EmployeeId = 1, EmployeeName = "Ndivhuwo Ranwedzi", Date = DateTime.Today, CheckIn = new TimeSpan(8, 0, 0), Status = AttendanceStatus.Present },
        new AttendanceRecord { Id = 2, EmployeeId = 2, EmployeeName = "Kopano Leshope", Date = DateTime.Today, CheckIn = new TimeSpan(8, 5, 0), Status = AttendanceStatus.Present },
        new AttendanceRecord { Id = 3, EmployeeId = 3, EmployeeName = "Karabo Dube", Date = DateTime.Today, CheckIn = new TimeSpan(8, 45, 0), Status = AttendanceStatus.Late },
        new AttendanceRecord { Id = 4, EmployeeId = 4, EmployeeName = "Kamogelo Mahube", Date = DateTime.Today, Status = AttendanceStatus.Absent },
        new AttendanceRecord { Id = 5, EmployeeId = 5, EmployeeName = "John Seleka", Date = DateTime.Today, Status = AttendanceStatus.OnLeave },
        // History for Michael Chen
        new AttendanceRecord { Id = 6, EmployeeId = 1, EmployeeName = "Ndivhuwo Ranwedzi", Date = DateTime.Today.AddDays(-1), CheckIn = new TimeSpan(3, 23, 0), CheckOut = new TimeSpan(12, 23, 0), Status = AttendanceStatus.Absent },
        new AttendanceRecord { Id = 7, EmployeeId = 1, EmployeeName = "Ndivhuwo Ranwedzi", Date = DateTime.Today.AddDays(-2), CheckIn = new TimeSpan(3, 23, 0), CheckOut = new TimeSpan(12, 23, 0), Status = AttendanceStatus.Present },
        new AttendanceRecord { Id = 8, EmployeeId = 1, EmployeeName = "Ndivhuwo Ranwedzi", Date = DateTime.Today.AddDays(-3), CheckIn = new TimeSpan(3, 23, 0), CheckOut = new TimeSpan(12, 23, 0), Status = AttendanceStatus.Present },
        new AttendanceRecord { Id = 9, EmployeeId = 1, EmployeeName = "Ndivhuwo Ranwedzi", Date = DateTime.Today.AddDays(-4), CheckIn = new TimeSpan(8, 0, 0), CheckOut = new TimeSpan(17, 0, 0), Status = AttendanceStatus.Present },
    };

        public static List<LeaveRequest> LeaveRequests { get; } = new()
    {
        new LeaveRequest { Id = 1, EmployeeId = 5, EmployeeName = "John Seleka", Initials = "JS", Type = LeaveType.Sick, Status = LeaveStatus.Approved, StartDate = new DateTime(2026, 5, 10), EndDate = new DateTime(2026, 5, 17), Reason = "Medical procedure and recovery", SubmittedDate = new DateTime(2026, 5, 9) },
        new LeaveRequest { Id = 2, EmployeeId = 1, EmployeeName = "Ndivhuwo Ranwedzi", Initials = "NR", Type = LeaveType.Annual, Status = LeaveStatus.Pending, StartDate = new DateTime(2026, 5, 26), EndDate = new DateTime(2026, 6, 2), Reason = "Family vacation", SubmittedDate = new DateTime(2026, 5, 11) },
        new LeaveRequest { Id = 3, EmployeeId = 4, EmployeeName = "Kamogelo Mahube", Initials = "KM", Type = LeaveType.Personal, Status = LeaveStatus.Pending, StartDate = new DateTime(2026, 5, 19), EndDate = new DateTime(2026, 5, 20), Reason = "Personal appointment", SubmittedDate = new DateTime(2026, 5, 10) },
    };

        public static List<Notification> Notifications { get; } = new()
    {
        new Notification { Id = 1, Title = "New Leave Request", Message = "Ndivhuwo Ranwedzi has submitted a leave request for review.", Type = "Info", Date = new DateTime(2026, 5, 11), IsRead = false },
        new Notification { Id = 2, Title = "Attendance Alert", Message = "Kevin Brown marked as absent today without prior notice.", Type = "Warning", Date = new DateTime(2026, 5, 12), IsRead = false },
        new Notification { Id = 3, Title = "System Update", Message = "New QR scanning feature has been enabled.", Type = "Success", Date = new DateTime(2026, 5, 5), IsRead = true },
    };

        public static List<WeeklyAttendance> WeeklyAttendanceData { get; } = new()
    {
        new WeeklyAttendance { Day = "Mon", Present = 45, Absent = 3, Late = 2 },
        new WeeklyAttendance { Day = "Tue", Present = 42, Absent = 5, Late = 3 },
        new WeeklyAttendance { Day = "Wed", Present = 44, Absent = 4, Late = 2 },
        new WeeklyAttendance { Day = "Thu", Present = 43, Absent = 4, Late = 3 },
        new WeeklyAttendance { Day = "Fri", Present = 40, Absent = 6, Late = 4 },
        new WeeklyAttendance { Day = "Sat", Present = 12, Absent = 2, Late = 1 },
        new WeeklyAttendance { Day = "Sun", Present = 8, Absent = 1, Late = 0 },
    };

        public static List<ProductivityRecord> ProductivityTrend { get; } = new()
    {
        new ProductivityRecord { Week = "Week 1", Target = 90, Actual = 92 },
        new ProductivityRecord { Week = "Week 2", Target = 90, Actual = 88 },
        new ProductivityRecord { Week = "Week 3", Target = 90, Actual = 91 },
        new ProductivityRecord { Week = "Week 4", Target = 90, Actual = 94 },
    };

        public static User? Authenticate(string email, string password)
            => Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == password);

        public static void ApproveLeave(int id)
        {
            var l = LeaveRequests.FirstOrDefault(x => x.Id == id);
            if (l != null) l.Status = LeaveStatus.Approved;
        }
        public static void RejectLeave(int id)
        {
            var l = LeaveRequests.FirstOrDefault(x => x.Id == id);
            if (l != null) l.Status = LeaveStatus.Rejected;
        }
    }
}
