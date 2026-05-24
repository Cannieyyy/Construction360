namespace Construction360.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int PendingLeaves { get; set; }
        public int OnLeave { get; set; }
        public int LateArrivals { get; set; }
        public double AvgProductivity { get; set; }
        public List<WeeklyAttendance> WeeklyAttendance { get; set; } = new();
        public Dictionary<string, double> DepartmentDistribution { get; set; } = new();
    }
}
