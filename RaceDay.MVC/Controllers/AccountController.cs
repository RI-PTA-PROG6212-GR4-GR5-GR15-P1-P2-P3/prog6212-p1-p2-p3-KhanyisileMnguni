using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiService _api;

        public AccountController(ApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.LoginAsync(model);

            if (result == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            HttpContext.Session.SetString("Token", result.Token);
            HttpContext.Session.SetString("UserId", result.UserId.ToString());
            HttpContext.Session.SetString("Name", result.Name);
            HttpContext.Session.SetString("Role", result.Role);

            if (result.Role == "Organiser")
                return RedirectToAction("Organiser", "Dashboard");

            return RedirectToAction("Participant", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var success = await _api.RegisterAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", "Registration failed. Check your details or try another email.");
                return View(model);
            }

            TempData["Message"] = "Registration successful. Please log in.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

