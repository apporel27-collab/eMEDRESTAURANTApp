using Microsoft.AspNetCore.Mvc;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Controllers
{
    public class CategoryController : Controller
    {
        private readonly RestaurantDbContext _db;
        public CategoryController(RestaurantDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromForm] Category model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed.";
                return RedirectToAction(nameof(Index));
            }
            model.Id = 0; // ensure new
            _db.Categories.Add(model);
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update([FromForm] Category model)
        {
            var existing = _db.Categories.FirstOrDefault(c => c.Id == model.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed.";
                return RedirectToAction(nameof(Index));
            }
            existing.Name = model.Name;
            existing.IsActive = model.IsActive;
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id)
        {
            var existing = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
            }
            else
            {
                existing.IsActive = !existing.IsActive;
                _db.SaveChanges();
                TempData["SuccessMessage"] = $"Category {(existing.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
