using Construction360.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Construction360.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseService _databaseService;

        public HomeController()
        {
            _databaseService = new DatabaseService();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
    }
}
