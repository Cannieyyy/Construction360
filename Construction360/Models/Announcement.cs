namespace Construction360.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public string Audience { get; set; } = "Everyone";

        public string SentBy { get; set; } = "";

        public DateTime SentDate { get; set; }

        public string Status { get; set; } = "Sent";
    }
}
