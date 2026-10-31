using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApiService _api;

        public EventsController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _api.GetEventsAsync();
            return View(events);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.CreateEventAsync(model, token);

            if (!success)
            {
                ModelState.AddModelError("", "Unable to create event. Please check your details.");
                return View(model);
            }

            TempData["Message"] = "Event created successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Categories(int id)
        {
            var categories = await _api.GetCategoriesAsync(id);

            ViewBag.EventId = id;

            return View(categories);
        }
        [HttpGet]
        public IActionResult AddCategory(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var model = new CategoryViewModel
            {
                EventID = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(CategoryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.AddCategoryAsync(
                model.EventID, model, token);

            if (!success)
            {
                ModelState.AddModelError("",
                    "Unable to add category. Please check your details.");

                return View(model);
            }

            TempData["Message"] = "Category added successfully!";

            return RedirectToAction("Categories", new { id = model.EventID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.EnrolAsync(categoryId, token);

            TempData["Message"] = success
                ? "You have successfully enrolled!"
                : "Unable to enrol. Please check your eligibility or try again.";

            return RedirectToAction("Categories", new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.DeleteEventAsync(id, token);

            TempData["Message"] = success
                ? "Event deleted successfully!"
                : "Unable to delete event. Please try again.";

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var enrolments = await _api.GetMyEnrolmentsAsync(token);

            return View(enrolments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.CancelEnrolmentAsync(id, token);

            TempData["Message"] = success
                ? "Your enrolment has been cancelled successfully!"
                : "Unable to cancel your enrolment. A result may already have been captured.";

            return RedirectToAction("MyEnrolments");
        }
        [HttpGet]
        public async Task<IActionResult> MyHistory()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var history = await _api.GetMyHistoryAsync(token);

            return View(history);
        }

        [HttpGet]
        public async Task<IActionResult> ManageParticipants(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var participants = await _api.GetEventEnrolmentsAsync(id, token);

            ViewBag.EventId = id;

            return View(participants);
        }

        [HttpGet]
        public IActionResult CaptureResult(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            return View(new ResultViewModel { EnrolmentId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(ResultViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.CaptureResultAsync(
                model.EnrolmentId, model, token);

            if (!success)
            {
                ModelState.AddModelError("", "Unable to capture the result. Please check the details.");
                return View(model);
            }

            TempData["Message"] = "Race result captured successfully!";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EditResult(int id, TimeSpan? finishTime, int? position, string? status)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var model = new ResultViewModel
            {
                EnrolmentId = id,
                FinishTime = finishTime,
                Position = position,
                Status = status ?? "Finished"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(ResultViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _api.UpdateResultAsync(
                model.EnrolmentId, model, token);

            if (!success)
            {
                ModelState.AddModelError("", "Unable to update the result. Please check the details.");
                return View(model);
            }

            TempData["Message"] = "Race result updated successfully!";

            return RedirectToAction("Index");
        }
    }
}
