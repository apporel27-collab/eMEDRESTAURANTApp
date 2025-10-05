using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Controllers
{
    public class SubCategoryController : Controller
    {
        private readonly RestaurantDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        
        public SubCategoryController(RestaurantDbContext db, IConfiguration configuration) 
        { 
            _db = db;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Method to ensure SubCategories table exists in dbo schema
        private bool EnsureDboSubCategoriesTableExists()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Check if SubCategories table exists in dbo schema
                var tableExistsQuery = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'SubCategories' AND TABLE_SCHEMA = 'dbo'";
                    
                using var checkCommand = new SqlCommand(tableExistsQuery, connection);
                var tableExists = (int)checkCommand.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    // Create the SubCategories table in dbo schema
                    var createTableSql = @"
                        CREATE TABLE [dbo].[SubCategories] (
                            Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            Name nvarchar(100) NOT NULL,
                            Description nvarchar(500) NULL,
                            IsActive bit NOT NULL DEFAULT 1,
                            CategoryId int NOT NULL,
                            DisplayOrder int NOT NULL DEFAULT 0,
                            CreatedAt datetime2 NOT NULL DEFAULT GETDATE(),
                            UpdatedAt datetime2 NULL,
                            CONSTRAINT FK_SubCategories_Categories 
                                FOREIGN KEY (CategoryId) 
                                REFERENCES [dbo].[Categories](Id)
                        );
                        
                        CREATE INDEX IX_SubCategories_CategoryId ON [dbo].[SubCategories](CategoryId);
                        CREATE INDEX IX_SubCategories_IsActive ON [dbo].[SubCategories](IsActive);";
                    
                    using var createCommand = new SqlCommand(createTableSql, connection);
                    createCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("Successfully created [dbo].[SubCategories] table");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring dbo SubCategories table exists: {ex.Message}");
                return false;
            }
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
                // Ensure dbo SubCategories table exists
                EnsureDboSubCategoriesTableExists();

                // Load SubCategories from dbo schema using raw SQL to avoid EF schema issues
                var subCategories = new List<SubCategory>();
                
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                
                var selectSql = @"
                    SELECT s.Id, s.Name, s.Description, s.IsActive, s.CategoryId, s.DisplayOrder, s.CreatedAt, s.UpdatedAt,
                           c.Name as CategoryName
                    FROM [dbo].[SubCategories] s
                    INNER JOIN [dbo].[Categories] c ON s.CategoryId = c.Id
                    ORDER BY c.Name, s.DisplayOrder, s.Name";
                
                using var command = new SqlCommand(selectSql, connection);
                using var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    var subCategory = new SubCategory
                    {
                        Id = reader.GetInt32("Id"),
                        Name = reader.GetString("Name"),
                        Description = reader.IsDBNull("Description") ? string.Empty : reader.GetString("Description"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CategoryId = reader.GetInt32("CategoryId"),
                        DisplayOrder = reader.GetInt32("DisplayOrder"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt"),
                        Category = new Category 
                        { 
                            Id = reader.GetInt32("CategoryId"), 
                            Name = reader.GetString("CategoryName") 
                        }
                    };
                    subCategories.Add(subCategory);
                }
                    
                return View(subCategories);
            }
            catch (Exception ex)
            {
                // Handle case when SubCategories table doesn't exist
                if (ex.Message.Contains("Invalid object name 'SubCategories'") || 
                    ex.Message.Contains("doesn't exist") || 
                    ex.Message.Contains("Could not find"))
                {
                    TempData["ErrorMessage"] = "SubCategories table not found. Attempting to create table automatically...";
                    
                    // Try to create the table
                    if (EnsureDboSubCategoriesTableExists())
                    {
                        TempData["SuccessMessage"] = "SubCategories table created successfully in dbo schema! You can now create sub categories.";
                        return View(new List<SubCategory>());
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Unable to create SubCategories table automatically in dbo schema. Please contact system administrator.";
                    }
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
                // Ensure dbo SubCategories table exists before proceeding
                if (!EnsureDboSubCategoriesTableExists())
                {
                    TempData["ErrorMessage"] = "Unable to create SubCategories table in dbo schema. Please contact system administrator.";
                    PopulateCategoriesDropdown(model?.CategoryId);
                    return View(model);
                }

                if (ModelState.IsValid)
                {
                    // Force Entity Framework to use dbo schema by executing raw SQL
                    using var connection = new SqlConnection(_connectionString);
                    connection.Open();
                    
                    // Check if sub category name already exists within the same category in dbo schema
                    var duplicateCheckSql = @"
                        SELECT COUNT(*) 
                        FROM [dbo].[SubCategories] 
                        WHERE Name = @Name AND CategoryId = @CategoryId";
                    
                    using var checkCommand = new SqlCommand(duplicateCheckSql, connection);
                    checkCommand.Parameters.AddWithValue("@Name", model.Name);
                    checkCommand.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    
                    var duplicateCount = (int)checkCommand.ExecuteScalar();
                    
                    if (duplicateCount > 0)
                    {
                        ModelState.AddModelError("Name", "A sub category with this name already exists in the selected category.");
                        PopulateCategoriesDropdown(model.CategoryId);
                        return View(model);
                    }

                    // Insert directly into dbo schema to bypass Entity Framework schema issues
                    var insertSql = @"
                        INSERT INTO [dbo].[SubCategories] (Name, Description, IsActive, CategoryId, DisplayOrder, CreatedAt, UpdatedAt)
                        VALUES (@Name, @Description, @IsActive, @CategoryId, @DisplayOrder, @CreatedAt, @UpdatedAt)";
                    
                    using var insertCommand = new SqlCommand(insertSql, connection);
                    insertCommand.Parameters.AddWithValue("@Name", model.Name);
                    insertCommand.Parameters.AddWithValue("@Description", model.Description ?? string.Empty);
                    insertCommand.Parameters.AddWithValue("@IsActive", model.IsActive);
                    insertCommand.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    insertCommand.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);
                    insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    insertCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    
                    insertCommand.ExecuteNonQuery();
                    
                    TempData["SuccessMessage"] = "Sub category created successfully!";
                    return RedirectToAction("Index");
                }

                PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }
            catch (Exception ex)
            {
                // Get more detailed error information
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner Exception: {ex.InnerException.Message}";
                }
                
                // Check for specific database-related errors
                if (ex.Message.Contains("Invalid object name") || 
                    ex.InnerException?.Message.Contains("Invalid object name") == true)
                {
                    TempData["ErrorMessage"] = "SubCategories table does not exist in the dbo schema. Please contact system administrator.";
                }
                else if (ex.Message.Contains("FOREIGN KEY") || ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
                {
                    TempData["ErrorMessage"] = "Invalid Category selected. Please select a valid category.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error creating sub category: {errorMessage}";
                }
                
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