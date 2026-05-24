using Construction360.Models;

namespace Construction360.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public string EmployeeName { get; set; } = "";
        public int DaysPresent { get; set; }
        public double MyEfficiency { get; set; }
        public int PendingLeaves { get; set; }
        public int LeaveBalance { get; set; }
        public AttendanceRecord? TodayAttendance { get; set; }
        public int TasksCompleted { get; set; }
        public int TotalTasks { get; set; }
    }
}
