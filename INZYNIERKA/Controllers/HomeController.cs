using INZYNIERKA.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace INZYNIERKA.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.UserName = User.Identity.Name;
            }
            return View();
        }
        /*
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return View(new ErrorViewModel {RequestId = requestId});
        }*/
    }
}