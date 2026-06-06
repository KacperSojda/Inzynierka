using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        public HomeController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    ViewBag.UserName = User.Identity.Name;
                    logger.LogInformation("Authenticated user {UserName} accessed the home page.", User.Identity.Name);
                }
                else
                {
                    logger.LogInformation("Unauthenticated user accessed the home page.");
                }

                return View();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while loading the home page.");
                TempData["ErrorMessage"] = "Unexpected error occurred.";
                return RedirectToAction("Error");
            }
        }
    }
}