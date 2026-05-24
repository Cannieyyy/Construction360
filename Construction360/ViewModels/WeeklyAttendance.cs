namespace Construction360.ViewModels
{
    public class WeeklyAttendance
    {
        public string Day { get; set; } = "";
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
    }
}
