namespace Construction360.Models
{
    public class LoginRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Initials { get; set; } = "";
        public DateTime LoginTime { get; set; }
        public string Location { get; set; } = "";
    }
}
