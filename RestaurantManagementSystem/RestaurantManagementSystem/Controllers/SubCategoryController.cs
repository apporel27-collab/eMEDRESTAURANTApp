using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Controllers
{
    public class SubCategoryController : Controller
    {
        private readonly RestaurantDbContext _db;
        
        public SubCategoryController(RestaurantDbContext db) 
        { 
            _db = db; 
        }

        // Helper method to populate categories dropdown
        private void PopulateCategoriesDropdown(int? selectedCategoryId = null)
        {
            ViewBag.CategoryId = new SelectList(
                _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name), 
                "Id", 
                "Name",
                selectedCategoryId
            );
        }

        // GET: SubCategory/Index
        public IActionResult Index()
        {
            try
            {
                // Check if SubCategories table exists
                var subCategories = _db.SubCategories
                    .Include(s => s.Category)
                    .OrderBy(s => s.Category.Name)
                    .ThenBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .ToList();
                    
                return View(subCategories);
            }
            catch (Exception ex)
            {
                // Handle case when SubCategories table doesn't exist
                if (ex.Message.Contains("Invalid object name 'SubCategories'") || 
                    ex.Message.Contains("doesn't exist") || 
                    ex.Message.Contains("Could not find"))
                {
                    TempData["ErrorMessage"] = "SubCategories table not found. Please run the database setup script (create_subcategories_table.sql) to create the required table structure.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error loading sub categories: {ex.Message}";
                }
                return View(new List<SubCategory>());
            }
        }

        // GET: SubCategory/Create
        public IActionResult Create()
        {
            try
            {
                PopulateCategoriesDropdown();
                
                var model = new SubCategory 
                { 
                    IsActive = true,
                    DisplayOrder = 0
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading create form: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: SubCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromForm] SubCategory model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if sub category name already exists within the same category
                    var existingSubCategory = _db.SubCategories
                        .FirstOrDefault(s => s.Name == model.Name && s.CategoryId == model.CategoryId);
                        
                    if (existingSubCategory != null)
                    {
                        ModelState.AddModelError("Name", "A sub category with this name already exists in the selected category.");
                        PopulateCategoriesDropdown(model.CategoryId);
                        return View(model);
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = null; // Set as null for new records
                    
                    _db.SubCategories.Add(model);
                    _db.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Sub category created successfully!";
                    return RedirectToAction("Index");
                }

                PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating sub category: {ex.Message}";
                PopulateCategoriesDropdown(model?.CategoryId);
                return View(model);
            }
        }

        // GET: SubCategory/Edit/5
        public IActionResult Edit(int id)
        {
            try
            {
                var subCategory = _db.SubCategories
                    .Include(s => s.Category)
                    .FirstOrDefault(s => s.Id == id);
                    
                if (subCategory == null)
                {
                    TempData["ErrorMessage"] = "Sub category not found.";
                    return RedirectToAction("Index");
                }

                PopulateCategoriesDropdown(subCategory.CategoryId);
                
                return View(subCategory);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading sub category: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: SubCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [FromForm] SubCategory model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["ErrorMessage"] = "Invalid sub category ID.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    // Check if sub category name already exists within the same category (excluding current record)
                    var existingSubCategory = _db.SubCategories
                        .FirstOrDefault(s => s.Name == model.Name && s.CategoryId == model.CategoryId && s.Id != model.Id);
                        
                    if (existingSubCategory != null)
                    {
                        ModelState.AddModelError("Name", "A sub category with this name already exists in the selected category.");
                        PopulateCategoriesDropdown(model.CategoryId);
                        return View(model);
                    }

                    var existingEntity = _db.SubCategories.Find(id);
                    if (existingEntity == null)
                    {
                        TempData["ErrorMessage"] = "Sub category not found.";
                        return RedirectToAction("Index");
                    }

                    // Update properties
                    existingEntity.Name = model.Name;
                    existingEntity.Description = model.Description ?? string.Empty;
                    existingEntity.CategoryId = model.CategoryId;
                    existingEntity.IsActive = model.IsActive;
                    existingEntity.DisplayOrder = model.DisplayOrder;
                    existingEntity.UpdatedAt = DateTime.Now;

                    _db.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Sub category updated successfully!";
                    return RedirectToAction("Index");
                }

                PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating sub category: {ex.Message}";
                PopulateCategoriesDropdown(model?.CategoryId);
                return View(model);
            }
        }

        // GET: SubCategory/Delete/5
        public IActionResult Delete(int id)
        {
            try
            {
                var subCategory = _db.SubCategories
                    .Include(s => s.Category)
                    .FirstOrDefault(s => s.Id == id);
                    
                if (subCategory == null)
                {
                    TempData["ErrorMessage"] = "Sub category not found.";
                    return RedirectToAction("Index");
                }

                return View(subCategory);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading sub category: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: SubCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var subCategory = _db.SubCategories.Find(id);
                if (subCategory == null)
                {
                    TempData["ErrorMessage"] = "Sub category not found.";
                    return RedirectToAction("Index");
                }

                // Check if sub category is being used (if you have menu items linked to sub categories)
                // You can add this check later when you link menu items to sub categories
                
                _db.SubCategories.Remove(subCategory);
                _db.SaveChanges();
                
                TempData["SuccessMessage"] = "Sub category deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting sub category: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: SubCategory/GetSubCategoriesByCategory/1
        public IActionResult GetSubCategoriesByCategory(int categoryId)
        {
            try
            {
                var subCategories = _db.SubCategories
                    .Where(s => s.CategoryId == categoryId && s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .Select(s => new { s.Id, s.Name })
                    .ToList();

                return Json(subCategories);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

                // POST: SubCategory/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            try
            {
                var subCategory = _db.SubCategories.Find(id);
                if (subCategory == null)
                {
                    return Json(new { success = false, message = "Sub category not found." });
                }

                subCategory.IsActive = !subCategory.IsActive;
                subCategory.UpdatedAt = DateTime.Now;
                _db.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = $"Sub category {(subCategory.IsActive ? "activated" : "deactivated")} successfully!",
                    isActive = subCategory.IsActive 
                });
            }
            catch (Exception ex)
            {
                // Handle database table not found error
                if (ex.Message.Contains("Invalid object name 'SubCategories'") || 
                    ex.Message.Contains("doesn't exist") || 
                    ex.Message.Contains("Could not find"))
                {
                    return Json(new { success = false, message = "SubCategories table not found. Please run the database setup script to create the required table structure." });
                }
                return Json(new { success = false, message = $"Error updating status: {ex.Message}" });
            }
        }
    }
}