using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

using WatchDogCompleteMVCTest.Models;

namespace WatchDogCompleteMVCTest.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) {
            _logger = logger;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Privacy() {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            _logger.LogInformation("Error method called");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
