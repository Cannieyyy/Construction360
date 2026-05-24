using Construction360.Enums;

namespace Construction360.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Initials { get; set; } = "";
        public LeaveType Type { get; set; }
        public LeaveStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = "";
        public int Days => (EndDate - StartDate).Days + 1;
        public DateTime SubmittedDate { get; set; }
    }
}
