using Construction360.Models;

namespace Construction360.ViewModels
{
    public class AnnouncementViewModel
    {

        public string Title { get; set; } = "";

        public string Message { get; set; } = "";

        public string Audience { get; set; } = "Everyone";

        public List<Announcement> Announcements { get; set; }
            = new List<Announcement>();
    }
}
