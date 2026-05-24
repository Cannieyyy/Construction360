using Construction360.Enums;

namespace Construction360.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public AttendanceStatus Status { get; set; }
        public double Hours => CheckIn.HasValue && CheckOut.HasValue ? (CheckOut.Value - CheckIn.Value).TotalHours : 0;
    }
}
