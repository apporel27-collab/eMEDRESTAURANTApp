using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.ViewModels;
using RestaurantManagementSystem.Helpers;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using ViewModelsMenuItemIngredientViewModel = RestaurantManagementSystem.ViewModels.MenuItemIngredientViewModel;
using ModelsMenuItemIngredientViewModel = RestaurantManagementSystem.Models.MenuItemIngredientViewModel;

namespace RestaurantManagementSystem.Controllers
{
    public class MenuController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MenuController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _webHostEnvironment = webHostEnvironment;
        }
        
        private bool HasColumn(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        
        // Safe getters for database values
        private int SafeGetInt(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch
            {
                return 0;
            }
        }
        
        private int? SafeGetNullableInt(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : (int?)reader.GetInt32(ordinal);
            }
            catch
            {
                return null;
            }
        }
        
        private string SafeGetString(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }
        
        private decimal SafeGetDecimal(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetDecimal(ordinal);
            }
            catch
            {
                return 0;
            }
        }
        
        private decimal? SafeGetNullableDecimal(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : (decimal?)reader.GetDecimal(ordinal);
            }
            catch
            {
                return null;
            }
        }
        
        private bool SafeGetBoolean(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? false : reader.GetBoolean(ordinal);
            }
            catch
            {
                return false;
            }
        }

        // GET: Menu
        public IActionResult Index()
        {
            var menuItems = GetAllMenuItems();
            return View(menuItems);
        }

        // GET: Menu/Details/5
        public IActionResult Details(int id)
        {
            var menuItem = GetMenuItemById(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // GET: Menu/Create
        public IActionResult Create()
        {
            ViewBag.Categories = GetCategorySelectList();
            ViewBag.Allergens = GetAllAllergens();
            // Ingredients tab removed; do not populate ViewBag.Ingredients
            ViewBag.Modifiers = GetAllModifiers();
            ViewBag.KitchenStations = GetKitchenStationSelectList();
            
            return View(new MenuItemViewModel());
        }

        // POST: Menu/Create
        [HttpPostAttribute]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult Create(MenuItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // New enhancement normalization
                    // If GST not applicable, ignore GSTPercentage value
                    if (!model.IsGstApplicable)
                    {
                        model.GSTPercentage = null;
                    }
                    // NotAvailable overrides IsAvailable
                    if (model.NotAvailable)
                    {
                        model.IsAvailable = false;
                    }
                    // Handle image upload if provided
                    if (model.ImageFile != null)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/menu");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(fileStream);
                        }
                        
                        model.ImagePath = "/images/menu/" + uniqueFileName;
                    }

                    // Create menu item
                    int menuItemId = CreateMenuItem(model);

                    // Add allergens
                    if (model.SelectedAllergens != null && model.SelectedAllergens.Any())
                    {
                        AddMenuItemAllergens(menuItemId, model.SelectedAllergens);
                    }

                    // Ingredients feature removed

                    // Add modifiers
                    if (model.SelectedModifiers != null && model.SelectedModifiers.Any())
                    {
                        AddMenuItemModifiers(menuItemId, model.SelectedModifiers, model.ModifierPrices);
                    }

                    TempData["SuccessMessage"] = "Menu item created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating menu item: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Categories = GetCategorySelectList();
            ViewBag.Allergens = GetAllAllergens();
            // Ingredients tab removed; do not populate ViewBag.Ingredients
            ViewBag.Modifiers = GetAllModifiers();
            ViewBag.KitchenStations = GetKitchenStationSelectList();
            
            return View(model);
        }

        // GET: Menu/Edit/5
        public IActionResult Edit(int id)
        {
            var menuItem = GetMenuItemById(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            // Convert to view model
            var viewModel = new MenuItemViewModel
            {
                Id = menuItem.Id,
                PLUCode = menuItem.PLUCode,
                Name = menuItem.Name,
                Description = menuItem.Description,
                Price = menuItem.Price,
                CategoryId = menuItem.CategoryId,
                ImagePath = menuItem.ImagePath,
                IsAvailable = menuItem.IsAvailable,
                NotAvailable = menuItem.NotAvailable,
                PreparationTimeMinutes = menuItem.PreparationTimeMinutes,
                CalorieCount = menuItem.CalorieCount,
                IsFeatured = menuItem.IsFeatured,
                IsSpecial = menuItem.IsSpecial,
                DiscountPercentage = menuItem.DiscountPercentage,
                KitchenStationId = menuItem.KitchenStationId,
                GSTPercentage = menuItem.GSTPercentage,
                IsGstApplicable = menuItem.IsGstApplicable,
                SelectedAllergens = menuItem.Allergens?.Select(a => a.AllergenId).ToList() ?? new List<int>(),
                // Ingredients feature removed from Edit view model mapping
                SelectedModifiers = menuItem.Modifiers?.Select(m => m.ModifierId).ToList() ?? new List<int>(),
                ModifierPrices = menuItem.Modifiers?.ToDictionary(m => m.ModifierId, m => m.PriceAdjustment) 
                    ?? new Dictionary<int, decimal>()
            };

            ViewBag.Categories = GetCategorySelectList();
            ViewBag.Allergens = GetAllAllergens();
            // Ingredients tab removed; do not populate ViewBag.Ingredients
            ViewBag.Modifiers = GetAllModifiers();
            ViewBag.KitchenStations = GetKitchenStationSelectList();
            
            return View(viewModel);
        }

        // POST: Menu/Edit/5
        [HttpPostAttribute]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult Edit(int id, MenuItemViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (!model.IsGstApplicable)
                    {
                        model.GSTPercentage = null;
                    }
                    if (model.NotAvailable)
                    {
                        model.IsAvailable = false;
                    }
                    // Handle image upload if provided
                    if (model.ImageFile != null)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/menu");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(fileStream);
                        }
                        
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(model.ImagePath))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, model.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                        
                        model.ImagePath = "/images/menu/" + uniqueFileName;
                    }

                    // Update menu item
                    UpdateMenuItem(model);

                    // Update allergens (remove all and add selected)
                    RemoveMenuItemAllergens(id);
                    if (model.SelectedAllergens != null && model.SelectedAllergens.Any())
                    {
                        AddMenuItemAllergens(id, model.SelectedAllergens);
                    }

                    // Ingredients feature removed

                    // Update modifiers (remove all and add selected)
                    RemoveMenuItemModifiers(id);
                    if (model.SelectedModifiers != null && model.SelectedModifiers.Any())
                    {
                        AddMenuItemModifiers(id, model.SelectedModifiers, model.ModifierPrices);
                    }

                    TempData["SuccessMessage"] = "Menu item updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating menu item: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Categories = GetCategorySelectList();
            ViewBag.Allergens = GetAllAllergens();
            // Ingredients tab removed; do not populate ViewBag.Ingredients
            ViewBag.Modifiers = GetAllModifiers();
            ViewBag.KitchenStations = GetKitchenStationSelectList();
            
            return View(model);
        }

        // GET: Menu/Delete/5
        public IActionResult Delete(int id)
        {
            var menuItem = GetMenuItemById(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // POST: Menu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                // Get image path before deleting
                var menuItem = GetMenuItemById(id);
                if (menuItem != null && !string.IsNullOrEmpty(menuItem.ImagePath))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, menuItem.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                DeleteMenuItem(id);
                TempData["SuccessMessage"] = "Menu item deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting menu item: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Menu/Recipe/5
        public IActionResult Recipe(int id)
        {
            var menuItem = GetMenuItemById(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            var recipe = GetRecipeByMenuItemId(id);
            var viewModel = new RecipeViewModel
            {
                MenuItemId = id,
                MenuItemName = menuItem.Name,
                Id = recipe?.Id ?? 0,
                Title = recipe?.Title ?? $"Recipe for {menuItem.Name}",
                PreparationInstructions = recipe?.PreparationInstructions ?? "",
                CookingInstructions = recipe?.CookingInstructions ?? "",
                PlatingInstructions = recipe?.PlatingInstructions ?? "",
                Yield = recipe?.Yield ?? 1,
                YieldPercentage = recipe?.YieldPercentage ?? 100,
                PreparationTimeMinutes = recipe?.PreparationTimeMinutes ?? menuItem.PreparationTimeMinutes,
                CookingTimeMinutes = recipe?.CookingTimeMinutes ?? 0,
                Notes = recipe?.Notes ?? "",
                IsArchived = recipe?.IsArchived ?? false,
                Version = recipe?.Version ?? 1,
                CreatedById = recipe?.CreatedById ?? 0,
                Steps = recipe?.Steps?.OrderBy(s => s.StepNumber).Select(s => new RecipeStepViewModel
                {
                    Id = s.Id,
                    RecipeId = recipe.Id,
                    StepNumber = s.StepNumber,
                    Description = s.Description,
                    TimeRequiredMinutes = s.TimeRequiredMinutes,
                    Temperature = s.Temperature,
                    SpecialEquipment = s.SpecialEquipment,
                    Tips = s.Tips,
                    ImagePath = s.ImagePath
                }).ToList() ?? new List<RecipeStepViewModel>()
            };

            // If no steps, add one empty step
            if (!viewModel.Steps.Any())
            {
                viewModel.Steps.Add(new RecipeStepViewModel { StepNumber = 1 });
            }

            return View(viewModel);
        }

        // POST: Menu/Recipe/5
        [HttpPostAttribute]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult Recipe(RecipeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Save recipe
                    int recipeId;
                    if (model.Id > 0)
                    {
                        // Update existing recipe
                        UpdateRecipe(model);
                        recipeId = model.Id;
                    }
                    else
                    {
                        // Create new recipe
                        recipeId = CreateRecipe(model);
                    }

                    // Handle step images and save steps
                    if (model.Steps != null)
                    {
                        foreach (var step in model.Steps)
                        {
                            if (step.ImageFile != null)
                            {
                                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/recipes");
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + step.ImageFile.FileName;
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                }
                                
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    step.ImageFile.CopyTo(fileStream);
                                }
                                
                                step.ImagePath = "/images/recipes/" + uniqueFileName;
                            }
                        }
                        
                        // Save steps
                        RemoveRecipeSteps(recipeId);
                        AddRecipeSteps(recipeId, model.Steps);
                    }

                    TempData["SuccessMessage"] = "Recipe saved successfully.";
                    return RedirectToAction(nameof(Details), new { id = model.MenuItemId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving recipe: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            var menuItem = GetMenuItemById(model.MenuItemId);
            model.MenuItemName = menuItem.Name;
            
            return View(model);
        }
        
        // Helper methods for database operations
        private List<MenuItem> GetAllMenuItems()
        {
            var menuItems = new List<MenuItem>();
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Instead of using the stored procedure which may not have all columns,
                // let's directly query the tables to ensure we get all columns
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    -- First, let's check if ItemType column exists
                    DECLARE @ItemTypeExists INT = 0;
                    
                    IF EXISTS (
                        SELECT 1
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'MenuItems' 
                        AND COLUMN_NAME = 'ItemType'
                    )
                    BEGIN
                        SET @ItemTypeExists = 1;
                    END
                    
                    -- Now build the dynamic query
                    DECLARE @SqlQuery NVARCHAR(MAX);
                    SET @SqlQuery = 'SELECT 
                        m.[Id], 
                        ISNULL(m.[PLUCode], '''') AS PLUCode,
                        m.[Name], 
                        m.[Description], 
                        m.[Price], 
                        m.[CategoryId], 
                        c.[Name] AS CategoryName,
                        m.[ImagePath], 
                        m.[IsAvailable], 
                        m.[NotAvailable],
                        ISNULL(m.[PrepTime], 0) AS PreparationTimeMinutes,
                        m.[CalorieCount],
                        ISNULL(m.[IsFeatured], 0) AS IsFeatured,
                        ISNULL(m.[IsSpecial], 0) AS IsSpecial,
                        m.[DiscountPercentage],
                        m.[KitchenStationId],
                        m.[TargetGP]';
                        
                    -- Include ItemType if it exists
                    IF @ItemTypeExists = 1
                    BEGIN
                        SET @SqlQuery = @SqlQuery + ', m.[ItemType]';
                    END
                    
                    SET @SqlQuery = @SqlQuery + '
                    FROM [dbo].[MenuItems] m
                    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
                    ORDER BY m.[Name]';
                    
                    EXEC sp_executesql @SqlQuery;", connection))
                {
                    try
                    {
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    var menuItem = new MenuItem
                                    {
                                        Id = SafeGetInt(reader, "Id"),
                                        PLUCode = SafeGetString(reader, "PLUCode") ?? string.Empty,
                                        Name = SafeGetString(reader, "Name") ?? string.Empty,
                                        Description = SafeGetString(reader, "Description") ?? string.Empty,
                                        Price = SafeGetDecimal(reader, "Price"),
                                        CategoryId = SafeGetInt(reader, "CategoryId"),
                                        Category = new Category { Name = SafeGetString(reader, "CategoryName") ?? "Uncategorized" },
                                        ImagePath = SafeGetString(reader, "ImagePath"),
                                        IsAvailable = SafeGetBoolean(reader, "IsAvailable"),
                                        NotAvailable = HasColumn(reader, "NotAvailable") ? SafeGetBoolean(reader, "NotAvailable") : false,
                                        PreparationTimeMinutes = SafeGetInt(reader, "PreparationTimeMinutes"),
                                        CalorieCount = SafeGetNullableInt(reader, "CalorieCount"),
                                        IsFeatured = SafeGetBoolean(reader, "IsFeatured"),
                                        IsSpecial = SafeGetBoolean(reader, "IsSpecial"),
                                        DiscountPercentage = SafeGetNullableDecimal(reader, "DiscountPercentage"),
                                        KitchenStationId = SafeGetNullableInt(reader, "KitchenStationId"),
                                        TargetGP = SafeGetNullableDecimal(reader, "TargetGP"),
                                        ItemType = HasColumn(reader, "ItemType") ? SafeGetString(reader, "ItemType") : null,
                                        IsGstApplicable = HasColumn(reader, "IsGstApplicable") ? SafeGetBoolean(reader, "IsGstApplicable") : true,
                                        GSTPercentage = HasColumn(reader, "GSTPercentage") ? SafeGetNullableDecimal(reader, "GSTPercentage") : (decimal?)5.00m,
                                        Allergens = new List<MenuItemAllergen>(),
                                        // Ingredients list is no longer loaded
                                        Modifiers = new List<MenuItemModifier>()
                                    };
                                    
                                    menuItems.Add(menuItem);
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but continue processing other records
                                    Console.WriteLine($"Error processing menu item: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        Console.WriteLine($"Error retrieving menu items: {ex.Message}");
                    }
                }
            }
            
            return menuItems;
        }
        
        private MenuItem GetMenuItemById(int id)
        {
            MenuItem menuItem = null;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        m.[Id], 
                        m.[PLUCode], 
                        m.[Name], 
                        m.[Description], 
                        m.[Price], 
                        m.[CategoryId], 
                        c.[Name] AS CategoryName,
                        m.[ImagePath], 
                        m.[IsAvailable], 
                        m.[PrepTime] AS PreparationTimeMinutes,
                        m.[CalorieCount],
                        m.[IsFeatured],
                        m.[IsSpecial],
                        m.[DiscountPercentage],
                        m.[KitchenStationId],
                        m.[TargetGP],
                        m.[GSTPercentage],
                        m.[IsGstApplicable]
                    FROM [dbo].[MenuItems] m
                    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
                    WHERE m.[Id] = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            menuItem = new MenuItem
                            {
                                Id = SafeGetInt(reader, "Id"),
                                PLUCode = SafeGetString(reader, "PLUCode") ?? string.Empty,
                                Name = SafeGetString(reader, "Name") ?? string.Empty,
                                Description = SafeGetString(reader, "Description") ?? string.Empty,
                                Price = SafeGetDecimal(reader, "Price"),
                                CategoryId = SafeGetInt(reader, "CategoryId"),
                                Category = new Category { Name = SafeGetString(reader, "CategoryName") ?? "Uncategorized" },
                                ImagePath = SafeGetString(reader, "ImagePath"),
                                IsAvailable = SafeGetBoolean(reader, "IsAvailable"),
                                NotAvailable = HasColumn(reader, "NotAvailable") ? SafeGetBoolean(reader, "NotAvailable") : false,
                                PreparationTimeMinutes = SafeGetInt(reader, "PreparationTimeMinutes"),
                                CalorieCount = SafeGetNullableInt(reader, "CalorieCount"),
                                IsFeatured = SafeGetBoolean(reader, "IsFeatured"),
                                IsSpecial = SafeGetBoolean(reader, "IsSpecial"),
                                DiscountPercentage = SafeGetNullableDecimal(reader, "DiscountPercentage"),
                                KitchenStationId = SafeGetNullableInt(reader, "KitchenStationId"),
                                TargetGP = SafeGetNullableDecimal(reader, "TargetGP"),
                                GSTPercentage = HasColumn(reader, "GSTPercentage") ? SafeGetNullableDecimal(reader, "GSTPercentage") : (decimal?)5.00m,
                                IsGstApplicable = HasColumn(reader, "IsGstApplicable") ? SafeGetBoolean(reader, "IsGstApplicable") : true,
                                Allergens = new List<MenuItemAllergen>(),
                                // Ingredients list is no longer loaded
                                Modifiers = new List<MenuItemModifier>()
                            };
                        }
                    }
                }
                
                if (menuItem != null)
                {
                    // Get allergens
                    try {
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                            SELECT mia.Id, mia.AllergenId, a.Name, mia.SeverityLevel
                            FROM MenuItemAllergens mia
                            JOIN Allergens a ON mia.AllergenId = a.Id
                            WHERE mia.MenuItemId = @MenuItemId", connection))
                        {
                            command.Parameters.AddWithValue("@MenuItemId", id);
                            
                            using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    menuItem.Allergens.Add(new MenuItemAllergen
                                    {
                                        Id = SafeGetInt(reader, "Id"),
                                        MenuItemId = id,
                                        AllergenId = SafeGetInt(reader, "AllergenId"),
                                        Allergen = new Allergen { Name = SafeGetString(reader, "Name") },
                                        SeverityLevel = SafeGetInt(reader, "SeverityLevel")
                                    });
                                }
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error getting allergens: {ex.Message}");
                    }
                    
                    // Skip ingredients loading intentionally
                    
                    // Get modifiers
                    try {
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                            SELECT mim.Id, mim.ModifierId, m.Name, mim.PriceAdjustment, mim.IsDefault, mim.MaxAllowed
                            FROM MenuItemModifiers mim
                            JOIN Modifiers m ON mim.ModifierId = m.Id
                            WHERE mim.MenuItemId = @MenuItemId", connection))
                        {
                            command.Parameters.AddWithValue("@MenuItemId", id);
                            
                            using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    menuItem.Modifiers.Add(new MenuItemModifier
                                    {
                                        Id = SafeGetInt(reader, "Id"),
                                        MenuItemId = id,
                                        ModifierId = SafeGetInt(reader, "ModifierId"),
                                        Modifier = new Modifier { Name = SafeGetString(reader, "Name") },
                                        PriceAdjustment = SafeGetDecimal(reader, "PriceAdjustment"),
                                        IsDefault = SafeGetBoolean(reader, "IsDefault"),
                                        MaxAllowed = SafeGetNullableInt(reader, "MaxAllowed")
                                    });
                                }
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error getting modifiers: {ex.Message}");
                    }
                }
            }
            
            return menuItem;
        }

        private int CreateMenuItem(MenuItemViewModel model)
        {
            int menuItemId = 0;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
            INSERT INTO MenuItems (PLUCode, Name, Description, Price, CategoryId, ImagePath,
                      IsAvailable, PrepTime, CalorieCount, 
                      IsFeatured, IsSpecial, DiscountPercentage, KitchenStationId, GSTPercentage, IsGstApplicable, NotAvailable)
                    VALUES (@PLUCode, @Name, @Description, @Price, @CategoryId, @ImagePath,
                @IsAvailable, @PreparationTimeMinutes, @CalorieCount, 
                @IsFeatured, @IsSpecial, @DiscountPercentage, @KitchenStationId, @GSTPercentage, @IsGstApplicable, @NotAvailable);
                    SELECT SCOPE_IDENTITY();", connection))
                {
                    command.Parameters.AddWithValue("@PLUCode", model.PLUCode);
                    command.Parameters.AddWithValue("@Name", model.Name);
                    command.Parameters.AddWithValue("@Description", model.Description);
                    command.Parameters.AddWithValue("@Price", model.Price);
                    command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        command.Parameters.AddWithValue("@ImagePath", model.ImagePath);
                    else
                        command.Parameters.AddWithValue("@ImagePath", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsAvailable", model.IsAvailable);
                    command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                    
                    if (model.CalorieCount.HasValue)
                        command.Parameters.AddWithValue("@CalorieCount", model.CalorieCount);
                    else
                        command.Parameters.AddWithValue("@CalorieCount", DBNull.Value);
                        
                    command.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);
                    command.Parameters.AddWithValue("@IsSpecial", model.IsSpecial);
                    
                    if (model.DiscountPercentage.HasValue)
                        command.Parameters.AddWithValue("@DiscountPercentage", model.DiscountPercentage);
                    else
                        command.Parameters.AddWithValue("@DiscountPercentage", DBNull.Value);
                        
                    if (model.KitchenStationId.HasValue)
                        command.Parameters.AddWithValue("@KitchenStationId", model.KitchenStationId);
                    else
                        command.Parameters.AddWithValue("@KitchenStationId", DBNull.Value);
                    
                    if (model.GSTPercentage.HasValue && model.IsGstApplicable)
                        command.Parameters.AddWithValue("@GSTPercentage", model.GSTPercentage);
                    else
                        command.Parameters.AddWithValue("@GSTPercentage", DBNull.Value);
                    command.Parameters.AddWithValue("@IsGstApplicable", model.IsGstApplicable);
                    command.Parameters.AddWithValue("@NotAvailable", model.NotAvailable);
                    
                    var result = command.ExecuteScalar();
                    menuItemId = Convert.ToInt32(result);
                }
            }
            
            return menuItemId;
        }

        private void UpdateMenuItem(MenuItemViewModel model)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    UPDATE MenuItems
                    SET Name = @Name,
                        PLUCode = @PLUCode,
                        Description = @Description,
                        Price = @Price,
                        CategoryId = @CategoryId,
                        ImagePath = @ImagePath,
                        IsAvailable = @IsAvailable,
                        PrepTime = @PreparationTimeMinutes,
                        CalorieCount = @CalorieCount,
                        IsFeatured = @IsFeatured,
                        IsSpecial = @IsSpecial,
                        DiscountPercentage = @DiscountPercentage,
                        KitchenStationId = @KitchenStationId,
                        GSTPercentage = @GSTPercentage,
                        IsGstApplicable = @IsGstApplicable,
                        NotAvailable = @NotAvailable
                    WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", model.Id);
                    command.Parameters.AddWithValue("@PLUCode", model.PLUCode ?? string.Empty);
                    command.Parameters.AddWithValue("@Name", model.Name);
                    command.Parameters.AddWithValue("@Description", model.Description);
                    command.Parameters.AddWithValue("@Price", model.Price);
                    command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        command.Parameters.AddWithValue("@ImagePath", model.ImagePath);
                    else
                        command.Parameters.AddWithValue("@ImagePath", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsAvailable", model.IsAvailable);
                    command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                    
                    if (model.CalorieCount.HasValue)
                        command.Parameters.AddWithValue("@CalorieCount", model.CalorieCount);
                    else
                        command.Parameters.AddWithValue("@CalorieCount", DBNull.Value);
                        
                    command.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);
                    command.Parameters.AddWithValue("@IsSpecial", model.IsSpecial);
                    
                    if (model.DiscountPercentage.HasValue)
                        command.Parameters.AddWithValue("@DiscountPercentage", model.DiscountPercentage);
                    else
                        command.Parameters.AddWithValue("@DiscountPercentage", DBNull.Value);
                        
                    if (model.KitchenStationId.HasValue)
                        command.Parameters.AddWithValue("@KitchenStationId", model.KitchenStationId);
                    else
                        command.Parameters.AddWithValue("@KitchenStationId", DBNull.Value);
                    
                    if (model.GSTPercentage.HasValue && model.IsGstApplicable)
                        command.Parameters.AddWithValue("@GSTPercentage", model.GSTPercentage);
                    else
                        command.Parameters.AddWithValue("@GSTPercentage", DBNull.Value);
                    command.Parameters.AddWithValue("@IsGstApplicable", model.IsGstApplicable);
                    command.Parameters.AddWithValue("@NotAvailable", model.NotAvailable);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteMenuItem(int id)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Resolve relationship tables (underscore/no-underscore)
                        string allergensTable = GetMenuItemRelationshipTableName(connection, transaction, "Allergens");
                        string ingredientsTable = GetMenuItemRelationshipTableName(connection, transaction, "Ingredients");
                        string modifiersTable = GetMenuItemRelationshipTableName(connection, transaction, "Modifiers");

                        // Delete allergens if table exists
                        if (TableExists(connection, transaction, allergensTable))
                        {
                            using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand($"DELETE FROM {allergensTable} WHERE MenuItemId = @MenuItemId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MenuItemId", id);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Delete ingredients if table exists
                        if (TableExists(connection, transaction, ingredientsTable))
                        {
                            using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand($"DELETE FROM {ingredientsTable} WHERE MenuItemId = @MenuItemId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MenuItemId", id);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Delete modifiers if table exists
                        if (TableExists(connection, transaction, modifiersTable))
                        {
                            using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand($"DELETE FROM {modifiersTable} WHERE MenuItemId = @MenuItemId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MenuItemId", id);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Delete menu item
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("DELETE FROM MenuItems WHERE Id = @Id", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void AddMenuItemAllergens(int menuItemId, List<int> allergenIds)
        {
            if (allergenIds == null || !allergenIds.Any())
                return;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var allergenId in allergenIds)
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        INSERT INTO MenuItemAllergens (MenuItemId, AllergenId, SeverityLevel)
                        VALUES (@MenuItemId, @AllergenId, @SeverityLevel)", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        command.Parameters.AddWithValue("@AllergenId", allergenId);
                        command.Parameters.AddWithValue("@SeverityLevel", 1); // Default severity
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void RemoveMenuItemAllergens(int menuItemId)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    DELETE FROM MenuItemAllergens WHERE MenuItemId = @MenuItemId", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddMenuItemIngredients(int menuItemId, List<Models.MenuItemIngredientViewModel> ingredients)
        {
            if (ingredients == null || !ingredients.Any())
                return;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var ingredient in ingredients)
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        INSERT INTO MenuItemIngredients (MenuItemId, IngredientId, Quantity, Unit, IsOptional, Instructions)
                        VALUES (@MenuItemId, @IngredientId, @Quantity, @Unit, @IsOptional, @Instructions)", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        command.Parameters.AddWithValue("@IngredientId", ingredient.IngredientId);
                        command.Parameters.AddWithValue("@Quantity", ingredient.Quantity);
                        command.Parameters.AddWithValue("@Unit", ingredient.Unit);
                        command.Parameters.AddWithValue("@IsOptional", ingredient.IsOptional);
                        
                        if (!string.IsNullOrEmpty(ingredient.Instructions))
                            command.Parameters.AddWithValue("@Instructions", ingredient.Instructions);
                        else
                            command.Parameters.AddWithValue("@Instructions", DBNull.Value);
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void RemoveMenuItemIngredients(int menuItemId)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Resolve correct table name and delete only if it exists
                string ingredientsTable = GetMenuItemRelationshipTableName(connection, null, "Ingredients");
                if (TableExists(connection, null, ingredientsTable))
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand($"DELETE FROM {ingredientsTable} WHERE MenuItemId = @MenuItemId", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void AddMenuItemModifiers(int menuItemId, List<int> modifierIds, Dictionary<int, decimal> modifierPrices)
        {
            if (modifierIds == null || !modifierIds.Any())
                return;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var modifierId in modifierIds)
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        INSERT INTO MenuItemModifiers (MenuItemId, ModifierId, PriceAdjustment, IsDefault, MaxAllowed)
                        VALUES (@MenuItemId, @ModifierId, @PriceAdjustment, @IsDefault, @MaxAllowed)", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        command.Parameters.AddWithValue("@ModifierId", modifierId);
                        
                        decimal priceAdjustment = 0;
                        if (modifierPrices != null && modifierPrices.ContainsKey(modifierId))
                        {
                            priceAdjustment = modifierPrices[modifierId];
                        }
                        command.Parameters.AddWithValue("@PriceAdjustment", priceAdjustment);
                        
                        command.Parameters.AddWithValue("@IsDefault", false); // Default value
                        command.Parameters.AddWithValue("@MaxAllowed", DBNull.Value);
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void RemoveMenuItemModifiers(int menuItemId)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                string modifiersTable = GetMenuItemRelationshipTableName(connection, null, "Modifiers");
                if (TableExists(connection, null, modifiersTable))
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand($"DELETE FROM {modifiersTable} WHERE MenuItemId = @MenuItemId", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        // Helpers: schema resilience for relationship tables
        private bool TableExists(Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction? transaction, string tableName)
        {
            try
            {
                using (Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT CASE WHEN OBJECT_ID(@t, 'U') IS NOT NULL THEN 1 ELSE 0 END", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@t", tableName);
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
            catch { return false; }
        }

        private string GetMenuItemRelationshipTableName(Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction? transaction, string relationship)
        {
            // Prefer underscore version if it exists, else fallback to non-underscore
            string withUnderscore = $"MenuItem_{relationship}";
            string withoutUnderscore = $"MenuItem{relationship}";

            if (TableExists(connection, transaction, withUnderscore))
                return withUnderscore;
            if (TableExists(connection, transaction, withoutUnderscore))
                return withoutUnderscore;
            // Default to non-underscore so callers can still build SQL safely if created later
            return withoutUnderscore;
        }

        private Recipe GetRecipeByMenuItemId(int menuItemId)
        {
            Recipe recipe = null;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                try
                {
                    // First try with stored procedure
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("sp_GetRecipeByMenuItemId", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        
                        try
                        {
                            using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    recipe = new Recipe
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                        MenuItemId = menuItemId,
                                        Title = reader.GetString(reader.GetOrdinal("Title")),
                                        PreparationInstructions = reader.GetString(reader.GetOrdinal("PreparationInstructions")),
                                        CookingInstructions = reader.GetString(reader.GetOrdinal("CookingInstructions")),
                                        PlatingInstructions = reader.IsDBNull(reader.GetOrdinal("PlatingInstructions")) ? null : reader.GetString(reader.GetOrdinal("PlatingInstructions")),
                                        Yield = reader.GetInt32(reader.GetOrdinal("Yield")),
                                        YieldPercentage = reader.GetDecimal(reader.GetOrdinal("YieldPercentage")),
                                        PreparationTimeMinutes = reader.GetInt32(reader.GetOrdinal("PreparationTimeMinutes")),
                                        CookingTimeMinutes = reader.GetInt32(reader.GetOrdinal("CookingTimeMinutes")),
                                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                                        IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                                        Version = reader.GetInt32(reader.GetOrdinal("Version")),
                                        CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("CreatedById")),
                                        Steps = new List<RecipeStep>()
                                    };
                                    return recipe;
                                }
                            }
                        }
                        catch
                        {
                            // Fallback to direct SQL query
                        }
                    }
                }
                catch
                {
                    // Fallback to direct SQL query
                }
                
                // Direct SQL query as fallback
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        r.[Id], 
                        r.[Title], 
                        r.[MenuItemId], 
                        r.[PreparationInstructions],
                        r.[CookingInstructions],
                        r.[PlatingInstructions],
                        r.[Yield],
                        r.[YieldPercentage],
                        r.[PreparationTimeMinutes],
                        r.[CookingTimeMinutes],
                        r.[Notes],
                        r.[IsArchived],
                        r.[Version],
                        r.[CreatedById]
                    FROM [dbo].[Recipes] r
                    WHERE r.[MenuItemId] = @MenuItemId", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            recipe = new Recipe
                            {
                                Id = SafeGetInt(reader, "Id"),
                                Title = SafeGetString(reader, "Title"),
                                MenuItemId = menuItemId,
                                PreparationInstructions = SafeGetString(reader, "PreparationInstructions"),
                                CookingInstructions = SafeGetString(reader, "CookingInstructions"),
                                PlatingInstructions = SafeGetString(reader, "PlatingInstructions"),
                                Yield = SafeGetInt(reader, "Yield"),
                                YieldPercentage = HasColumn(reader, "YieldPercentage") ? SafeGetDecimal(reader, "YieldPercentage") : 100,
                                PreparationTimeMinutes = SafeGetInt(reader, "PreparationTimeMinutes"),
                                CookingTimeMinutes = SafeGetInt(reader, "CookingTimeMinutes"),
                                Notes = SafeGetString(reader, "Notes"),
                                IsArchived = SafeGetBoolean(reader, "IsArchived"),
                                Version = SafeGetInt(reader, "Version"),
                                CreatedById = HasColumn(reader, "CreatedById") ? SafeGetNullableInt(reader, "CreatedById") : null,
                                Steps = new List<RecipeStep>()
                            };
                        }
                    }
                }
                
                if (recipe != null)
                {
                    // Get recipe steps
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        SELECT 
                            rs.[Id],
                            rs.[RecipeId],
                            rs.[StepNumber],
                            rs.[Description],
                            rs.[TimeRequiredMinutes],
                            rs.[Temperature],
                            rs.[SpecialEquipment],
                            rs.[Tips],
                            rs.[ImagePath]
                        FROM [dbo].[RecipeSteps] rs
                        WHERE rs.[RecipeId] = @RecipeId
                        ORDER BY rs.[StepNumber]", connection))
                    {
                        command.Parameters.AddWithValue("@RecipeId", recipe.Id);
                        
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                recipe.Steps.Add(new RecipeStep
                                {
                                    Id = SafeGetInt(reader, "Id"),
                                    RecipeId = recipe.Id,
                                    StepNumber = SafeGetInt(reader, "StepNumber"),
                                    Description = SafeGetString(reader, "Description"),
                                    TimeRequiredMinutes = SafeGetNullableInt(reader, "TimeRequiredMinutes"),
                                    Temperature = SafeGetString(reader, "Temperature"),
                                    SpecialEquipment = SafeGetString(reader, "SpecialEquipment"),
                                    Tips = SafeGetString(reader, "Tips"),
                                    ImagePath = SafeGetString(reader, "ImagePath")
                                });
                            }
                        }
                    }
                }
            }
            
            return recipe;
        }

        private int CreateRecipe(RecipeViewModel model)
        {
            int recipeId = 0;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    INSERT INTO Recipes (Title, MenuItemId, PreparationInstructions, CookingInstructions,
                                         PlatingInstructions, Yield, YieldPercentage, PreparationTimeMinutes, CookingTimeMinutes,
                                         Notes, IsArchived, Version, CreatedById, LastUpdated)
                    VALUES (@Title, @MenuItemId, @PreparationInstructions, @CookingInstructions,
                            @PlatingInstructions, @Yield, @YieldPercentage, @PreparationTimeMinutes, @CookingTimeMinutes,
                            @Notes, @IsArchived, @Version, @CreatedById, @LastUpdated);
                    SELECT SCOPE_IDENTITY();", connection))
                {
                    command.Parameters.AddWithValue("@Title", model.Title);
                    command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                    command.Parameters.AddWithValue("@PreparationInstructions", model.PreparationInstructions ?? "");
                    command.Parameters.AddWithValue("@CookingInstructions", model.CookingInstructions ?? "");
                    
                    if (!string.IsNullOrEmpty(model.PlatingInstructions))
                        command.Parameters.AddWithValue("@PlatingInstructions", model.PlatingInstructions);
                    else
                        command.Parameters.AddWithValue("@PlatingInstructions", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@Yield", model.Yield);
                    command.Parameters.AddWithValue("@YieldPercentage", model.YieldPercentage);
                    command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                    command.Parameters.AddWithValue("@CookingTimeMinutes", model.CookingTimeMinutes);
                    
                    if (!string.IsNullOrEmpty(model.Notes))
                        command.Parameters.AddWithValue("@Notes", model.Notes);
                    else
                        command.Parameters.AddWithValue("@Notes", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsArchived", model.IsArchived);
                    command.Parameters.AddWithValue("@Version", model.Version);
                    
                    if (model.CreatedById > 0)
                        command.Parameters.AddWithValue("@CreatedById", model.CreatedById);
                    else
                        command.Parameters.AddWithValue("@CreatedById", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@LastUpdated", DateTime.Now);
                    
                    var result = command.ExecuteScalar();
                    recipeId = Convert.ToInt32(result);
                }
            }
            
            return recipeId;
        }

        private void UpdateRecipe(RecipeViewModel model)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    UPDATE Recipes
                    SET Title = @Title,
                        PreparationInstructions = @PreparationInstructions,
                        CookingInstructions = @CookingInstructions,
                        PlatingInstructions = @PlatingInstructions,
                        Yield = @Yield,
                        YieldPercentage = @YieldPercentage,
                        PreparationTimeMinutes = @PreparationTimeMinutes,
                        CookingTimeMinutes = @CookingTimeMinutes,
                        Notes = @Notes,
                        IsArchived = @IsArchived,
                        Version = @Version,
                        LastUpdated = @LastUpdated
                    WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", model.Id);
                    command.Parameters.AddWithValue("@Title", model.Title);
                    command.Parameters.AddWithValue("@PreparationInstructions", model.PreparationInstructions ?? "");
                    command.Parameters.AddWithValue("@CookingInstructions", model.CookingInstructions ?? "");
                    
                    if (!string.IsNullOrEmpty(model.PlatingInstructions))
                        command.Parameters.AddWithValue("@PlatingInstructions", model.PlatingInstructions);
                    else
                        command.Parameters.AddWithValue("@PlatingInstructions", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@Yield", model.Yield);
                    command.Parameters.AddWithValue("@YieldPercentage", model.YieldPercentage);
                    command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                    command.Parameters.AddWithValue("@CookingTimeMinutes", model.CookingTimeMinutes);
                    
                    if (!string.IsNullOrEmpty(model.Notes))
                        command.Parameters.AddWithValue("@Notes", model.Notes);
                    else
                        command.Parameters.AddWithValue("@Notes", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsArchived", model.IsArchived);
                    command.Parameters.AddWithValue("@Version", model.Version);
                    command.Parameters.AddWithValue("@LastUpdated", DateTime.Now);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddRecipeSteps(int recipeId, List<RecipeStepViewModel> steps)
        {
            if (steps == null || !steps.Any())
                return;
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var step in steps)
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        INSERT INTO RecipeSteps (RecipeId, StepNumber, Description, TimeRequiredMinutes,
                                               Temperature, SpecialEquipment, Tips, ImagePath)
                        VALUES (@RecipeId, @StepNumber, @Description, @TimeRequiredMinutes,
                               @Temperature, @SpecialEquipment, @Tips, @ImagePath)", connection))
                    {
                        command.Parameters.AddWithValue("@RecipeId", recipeId);
                        command.Parameters.AddWithValue("@StepNumber", step.StepNumber);
                        command.Parameters.AddWithValue("@Description", step.Description ?? "");
                        
                        if (step.TimeRequiredMinutes.HasValue)
                            command.Parameters.AddWithValue("@TimeRequiredMinutes", step.TimeRequiredMinutes);
                        else
                            command.Parameters.AddWithValue("@TimeRequiredMinutes", DBNull.Value);
                        
                        if (!string.IsNullOrEmpty(step.Temperature))
                            command.Parameters.AddWithValue("@Temperature", step.Temperature);
                        else
                            command.Parameters.AddWithValue("@Temperature", DBNull.Value);
                        
                        if (!string.IsNullOrEmpty(step.SpecialEquipment))
                            command.Parameters.AddWithValue("@SpecialEquipment", step.SpecialEquipment);
                        else
                            command.Parameters.AddWithValue("@SpecialEquipment", DBNull.Value);
                        
                        if (!string.IsNullOrEmpty(step.Tips))
                            command.Parameters.AddWithValue("@Tips", step.Tips);
                        else
                            command.Parameters.AddWithValue("@Tips", DBNull.Value);
                        
                        if (!string.IsNullOrEmpty(step.ImagePath))
                            command.Parameters.AddWithValue("@ImagePath", step.ImagePath);
                        else
                            command.Parameters.AddWithValue("@ImagePath", DBNull.Value);
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void RemoveRecipeSteps(int recipeId)
        {
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    DELETE FROM RecipeSteps WHERE RecipeId = @RecipeId", connection))
                {
                    command.Parameters.AddWithValue("@RecipeId", recipeId);
                    command.ExecuteNonQuery();
                }
            }
        }
        private List<SelectListItem> GetCategorySelectList()
        {
            var categories = new List<SelectListItem>();
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT Id, Name
                    FROM Categories
                    ORDER BY Name", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            
            return categories;
        }

        private List<Allergen> GetAllAllergens()
        {
            var allergens = new List<Allergen>();
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT Id, Name, Description
                    FROM Allergens
                    ORDER BY Name", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allergens.Add(new Allergen
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : null
                            });
                        }
                    }
                }
            }
            
            return allergens;
        }

        private List<SelectListItem> GetIngredientSelectList()
        {
            var ingredients = new List<SelectListItem>();
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT Id, IngredientsName, DisplayName
                    FROM Ingredients
                    ORDER BY IngredientsName", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ingredients.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = !reader.IsDBNull(2) && !string.IsNullOrEmpty(reader.GetString(2)) 
                                    ? reader.GetString(2) 
                                    : reader.GetString(1)
                            });
                        }
                    }
                }
            }
            
            return ingredients;
        }

        private List<Modifier> GetAllModifiers()
        {
            var modifiers = new List<Modifier>();
            
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        SELECT Id, Name, Description
                        FROM Modifiers
                        ORDER BY Name", connection))
                    {
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                modifiers.Add(new Modifier
                                {
                                    Id = SafeGetInt(reader, "Id"),
                                    Name = SafeGetString(reader, "Name"),
                                    Description = SafeGetString(reader, "Description")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting modifiers: {ex.Message}");
            }
            
            return modifiers;
        }

        private List<SelectListItem> GetKitchenStationSelectList()
        {
            var stations = new List<SelectListItem>();
            
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        SELECT Id, Name
                        FROM KitchenStations
                        ORDER BY Name", connection))
                    {
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                stations.Add(new SelectListItem
                                {
                                    Value = SafeGetInt(reader, "Id").ToString(),
                                    Text = SafeGetString(reader, "Name")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting kitchen stations: {ex.Message}");
                // Add an empty selection in case of error
                stations.Add(new SelectListItem
                {
                    Value = "",
                    Text = "-- Select Station --"
                });
            }
            
            return stations;
        }

        private List<ModelsMenuItemIngredientViewModel> ConvertIngredientsViewModelToModel(
            List<ViewModelsMenuItemIngredientViewModel> viewModelIngredients)
        {
            return viewModelIngredients.Select(i => new ModelsMenuItemIngredientViewModel
            {
                IngredientId = i.IngredientId,
                Quantity = i.Quantity,
                Unit = i.Unit,
                IsOptional = i.IsOptional,
                Instructions = i.Instructions
            }).ToList();
        }
    }
}
