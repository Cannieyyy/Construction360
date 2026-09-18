using Construction360.Models;
namespace Construction360.ViewModels
{
    public class LoginTrackerViewModel
    {
        public int TotalLogins { get; set; }

        public int TodayLogins { get; set; }

        public int RecentLogins { get; set; }

        public int UniqueLocations { get; set; }

        public List<LoginTrackerItemViewModel> Logins { get; set; }
            = new List<LoginTrackerItemViewModel>();
    }
}
