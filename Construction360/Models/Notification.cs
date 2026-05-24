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
    }
}
