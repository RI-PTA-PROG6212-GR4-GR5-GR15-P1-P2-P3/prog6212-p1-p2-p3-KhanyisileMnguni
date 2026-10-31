using Microsoft.AspNetCore.Mvc;

namespace RaceDay.MVC.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Organiser()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            return View();
        }

        public IActionResult Participant()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            return View();
        }
    }
}
