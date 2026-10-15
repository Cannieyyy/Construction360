namespace Construction360.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
        public DateTime Date { get; set; }
        public bool IsRead { get; set; }

        // New fields
        public int? UserId { get; set; }           // Null = broadcast to all
        public string? Audience { get; set; }      // "Everyone", "Admin", "Supervisor", "Employee"
        public string? SentBy { get; set; }        // Name of the sender
    }
}