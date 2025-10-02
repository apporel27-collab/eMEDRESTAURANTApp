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
        public IActionResult Save([FromForm] Category model)
        {
            // Manual validation (avoid silent model binding issues)
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                var errs = ModelState.Where(kv => kv.Value?.Errors.Count > 0)
                    .Select(kv => $"{kv.Key}: {string.Join("; ", kv.Value!.Errors.Select(e => e.ErrorMessage))}");
                TempData["ErrorMessage"] = "Validation failed: " + string.Join(" | ", errs);
                return RedirectToAction(nameof(Index));
            }
            if (model.Id == 0)
            {
                _db.Categories.Add(model);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Category created.";
            }
            else
            {
                var existing = _db.Categories.FirstOrDefault(c => c.Id == model.Id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }
                existing.Name = model.Name;
                existing.IsActive = model.IsActive;
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Category updated.";
            }
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
