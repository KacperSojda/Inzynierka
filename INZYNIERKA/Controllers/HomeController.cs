using Microsoft.AspNetCore.Mvc;

namespace INZYNIERKA.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    ViewBag.UserName = User.Identity.Name;
                }
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error");
            }
        }
    }
}