namespace Construction360.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Status { get; set; } = "Active";
        public string Initials => string.Concat(FullName.Split(' ').Select(w => w.Length > 0 ? w[0].ToString() : ""));
    }
}
