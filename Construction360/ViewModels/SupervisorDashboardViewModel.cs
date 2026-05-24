using Construction360.Models;

namespace Construction360.ViewModels
{
    public class SupervisorDashboardViewModel
    {
        public int TeamSize { get; set; }
        public int PresentToday { get; set; }
        public int PendingApprovals { get; set; }
        public double TeamProductivity { get; set; }
        public List<WeeklyAttendance> WeeklyAttendance { get; set; } = new();
        public List<ProductivityRecord> ProductivityTrend { get; set; } = new();
    }
}
