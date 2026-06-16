using HotelManagement.Models;
using HotelManagement.Services.Customer;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HotelManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PublicHomeService _publicHomeService;

        public HomeController(
            ILogger<HomeController> logger,
            PublicHomeService publicHomeService)
        {
            _logger = logger;
            _publicHomeService = publicHomeService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _publicHomeService.GetHomePageAsync();
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
