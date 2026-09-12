using Construction360.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Construction360.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseService _databaseService;

        public HomeController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
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
