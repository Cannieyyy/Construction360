using Construction360.Models;

namespace Construction360.ViewModels
{
    public class SupervisorDashboardViewModel
    {
        // Key Metrics
        public int TeamSize { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int OnLeaveToday { get; set; }
        public int PendingApprovals { get; set; }
        public double TeamProductivity { get; set; }
        public double ProductivityChange { get; set; }

        // Charts Data 
        public List<WeeklyAttendance> WeeklyAttendance { get; set; } = new();
        public List<ProductivityRecord> ProductivityTrend { get; set; } = new();

        // Recent Data 
        public List<LeaveRequest> RecentLeaveRequests { get; set; } = new();
        public List<AttendanceRecord> RecentAttendance { get; set; } = new();

        // Quick Stattistics
        public double AvgEfficiency { get; set; }
        public int TotalTasksCompleted { get; set; }
        public int TotalHoursWorked { get; set; }
        public string TopPerformer { get; set; } = "";
    }
}