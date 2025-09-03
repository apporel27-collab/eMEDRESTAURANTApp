using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.ViewModels;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using MenuItemIngredientViewModelView = RestaurantManagementSystem.ViewModels.MenuItemIngredientViewModel;
using MenuItemIngredientViewModelModel = RestaurantManagementSystem.Models.MenuItemIngredientViewModel;

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
            ViewBag.Ingredients = GetIngredientSelectList();
            ViewBag.Modifiers = GetAllModifiers();
            ViewBag.KitchenStations = GetKitchenStationSelectList();
            
            return View(new MenuItemViewModel());
        }

        // POST: Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MenuItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
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

                    // Add ingredients
                    if (model.Ingredients != null && model.Ingredients.Any())
                    {
                        var modelIngredients = ConvertIngredientsViewModelToModel(model.Ingredients);
                        AddMenuItemIngredients(menuItemId, modelIngredients);
                    }

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
            ViewBag.Ingredients = GetIngredientSelectList();
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
                PreparationTimeMinutes = menuItem.PreparationTimeMinutes,
                CalorieCount = menuItem.CalorieCount,
                IsFeatured = menuItem.IsFeatured,
                IsSpecial = menuItem.IsSpecial,
                DiscountPercentage = menuItem.DiscountPercentage,
                KitchenStationId = menuItem.KitchenStationId,
                SelectedAllergens = menuItem.Allergens.Select(a => a.AllergenId).ToList(),
                Ingredients = menuItem.Ingredients.Select(i => new RestaurantManagementSystem.ViewModels.MenuItemIngredientViewModel
                {
                    IngredientId = i.IngredientId,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    IsOptional = i.IsOptional,
                    Instructions = i.Instructions
                }).ToList(),
                SelectedModifiers = menuItem.Modifiers.Select(m => m.ModifierId).ToList(),
                ModifierPrices = menuItem.Modifiers.ToDictionary(m => m.ModifierId, m => m.PriceAdjustment)
            };

            ViewBag.Categories = GetCategorySelectList();
            ViewBag.Allergens = GetAllAllergens();
            ViewBag.Ingredients = GetIngredientSelectList();
            ViewBag.Modifiers = GetAllModifiers();
            ViewBag.KitchenStations = GetKitchenStationSelectList();
            
            return View(viewModel);
        }

        // POST: Menu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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

                    // Update ingredients (remove all and add selected)
                    RemoveMenuItemIngredients(id);
                    if (model.Ingredients != null && model.Ingredients.Any())
                    {
                        // Convert from ViewModels.MenuItemIngredientViewModel to Models.MenuItemIngredientViewModel
                        var modelIngredients = model.Ingredients.Select(i => new Models.MenuItemIngredientViewModel
                        {
                            IngredientId = i.IngredientId,
                            Quantity = i.Quantity,
                            Unit = i.Unit,
                            IsOptional = i.IsOptional,
                            Instructions = i.Instructions
                        }).ToList();
                        
                        AddMenuItemIngredients(id, modelIngredients);
                    }

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
            ViewBag.Ingredients = GetIngredientSelectList();
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
        [ValidateAntiForgeryToken]
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
                PreparationTimeMinutes = recipe?.PreparationTimeMinutes ?? menuItem.PreparationTimeMinutes,
                CookingTimeMinutes = recipe?.CookingTimeMinutes ?? 0,
                Notes = recipe?.Notes ?? "",
                IsArchived = recipe?.IsArchived ?? false,
                Version = recipe?.Version ?? 1,
                Steps = recipe?.Steps.OrderBy(s => s.StepNumber).Select(s => new RecipeStepViewModel
                {
                    Id = s.Id,
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
        [HttpPost]
        [ValidateAntiForgeryToken]
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

                        // Remove existing steps
                        RemoveRecipeSteps(recipeId);

                        // Add updated steps
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
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand("sp_GetAllMenuItems", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            menuItems.Add(new MenuItem
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                PLUCode = reader.GetString(reader.GetOrdinal("PLUCode")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                Category = new Category { Name = reader.GetString(reader.GetOrdinal("CategoryName")) },
                                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                                IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                                PreparationTimeMinutes = reader.GetInt32(reader.GetOrdinal("PreparationTimeMinutes")),
                                CalorieCount = reader.IsDBNull(reader.GetOrdinal("CalorieCount")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("CalorieCount")),
                                IsFeatured = reader.GetBoolean(reader.GetOrdinal("IsFeatured")),
                                IsSpecial = reader.GetBoolean(reader.GetOrdinal("IsSpecial")),
                                DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("DiscountPercentage")),
                                TargetGP = reader.IsDBNull(reader.GetOrdinal("TargetGP")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("TargetGP"))
                            });
                        }
                    }
                }
            }
            
            return menuItems;
        }

        private MenuItem GetMenuItemById(int id)
        {
            MenuItem menuItem = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get menu item details using stored procedure
                using (SqlCommand command = new SqlCommand("sp_GetMenuItemById", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            menuItem = new MenuItem
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                PLUCode = reader.GetString(reader.GetOrdinal("PLUCode")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                Category = new Category { Name = reader.GetString(reader.GetOrdinal("CategoryName")) },
                                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                                IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                                PreparationTimeMinutes = reader.GetInt32(reader.GetOrdinal("PreparationTimeMinutes")),
                                CalorieCount = reader.IsDBNull(reader.GetOrdinal("CalorieCount")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("CalorieCount")),
                                IsFeatured = reader.GetBoolean(reader.GetOrdinal("IsFeatured")),
                                IsSpecial = reader.GetBoolean(reader.GetOrdinal("IsSpecial")),
                                DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("DiscountPercentage")),
                                TargetGP = reader.IsDBNull(reader.GetOrdinal("TargetGP")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("TargetGP")),
                                Allergens = new List<MenuItemAllergen>(),
                                Ingredients = new List<MenuItemIngredient>(),
                                Modifiers = new List<MenuItemModifier>()
                            };
                        }
                    }
                }
                
                if (menuItem != null)
                {
                    // Get allergens
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT mia.Id, mia.AllergenId, a.Name, mia.SeverityLevel
                        FROM MenuItemAllergens mia
                        JOIN Allergens a ON mia.AllergenId = a.Id
                        WHERE mia.MenuItemId = @MenuItemId", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", id);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                menuItem.Allergens.Add(new MenuItemAllergen
                                {
                                    Id = reader.GetInt32(0),
                                    MenuItemId = id,
                                    AllergenId = reader.GetInt32(1),
                                    Allergen = new Allergen { Name = reader.GetString(2) },
                                    SeverityLevel = reader.GetInt32(3)
                                });
                            }
                        }
                    }
                    
                    // Get ingredients
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT mii.Id, mii.IngredientId, i.Name, mii.Quantity, mii.Unit, mii.IsOptional, mii.Instructions
                        FROM MenuItemIngredients mii
                        JOIN Ingredients i ON mii.IngredientId = i.Id
                        WHERE mii.MenuItemId = @MenuItemId", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", id);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                menuItem.Ingredients.Add(new MenuItemIngredient
                                {
                                    Id = reader.GetInt32(0),
                                    MenuItemId = id,
                                    IngredientId = reader.GetInt32(1),
                                    Ingredient = new Ingredients { IngredientsName = reader.GetString(2) },
                                    Quantity = reader.GetDecimal(3),
                                    Unit = reader.GetString(4),
                                    IsOptional = reader.GetBoolean(5),
                                    Instructions = reader.IsDBNull(6) ? null : reader.GetString(6)
                                });
                            }
                        }
                    }
                    
                    // Get modifiers
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT mim.Id, mim.ModifierId, m.Name, mim.PriceAdjustment, mim.IsDefault, mim.MaxAllowed
                        FROM MenuItemModifiers mim
                        JOIN Modifiers m ON mim.ModifierId = m.Id
                        WHERE mim.MenuItemId = @MenuItemId", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", id);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                menuItem.Modifiers.Add(new MenuItemModifier
                                {
                                    Id = reader.GetInt32(0),
                                    MenuItemId = id,
                                    ModifierId = reader.GetInt32(1),
                                    Modifier = new Modifier { Name = reader.GetString(2) },
                                    PriceAdjustment = reader.GetDecimal(3),
                                    IsDefault = reader.GetBoolean(4),
                                    MaxAllowed = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5)
                                });
                            }
                        }
                    }
                }
            }
            
            return menuItem;
        }

        private int CreateMenuItem(MenuItemViewModel model)
        {
            int menuItemId;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    INSERT INTO MenuItems (PLUCode, Name, Description, Price, CategoryId, ImagePath,
                                          IsAvailable, PreparationTimeMinutes, CalorieCount, 
                                          IsFeatured, IsSpecial, DiscountPercentage, KitchenStationId)
                    VALUES (@PLUCode, @Name, @Description, @Price, @CategoryId, @ImagePath,
                            @IsAvailable, @PreparationTimeMinutes, @CalorieCount, 
                            @IsFeatured, @IsSpecial, @DiscountPercentage, @KitchenStationId);
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
                    
                    menuItemId = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            
            return menuItemId;
        }

        private void UpdateMenuItem(MenuItemViewModel model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    UPDATE MenuItems
                    SET Name = @Name,
                        Description = @Description,
                        Price = @Price,
                        CategoryId = @CategoryId,
                        ImagePath = @ImagePath,
                        IsAvailable = @IsAvailable
                    WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", model.Id);
                    command.Parameters.AddWithValue("@Name", model.Name);
                    command.Parameters.AddWithValue("@Description", model.Description);
                    command.Parameters.AddWithValue("@Price", model.Price);
                    command.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        command.Parameters.AddWithValue("@ImagePath", model.ImagePath);
                    else
                        command.Parameters.AddWithValue("@ImagePath", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsAvailable", model.IsAvailable);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        private void DeleteMenuItem(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    DELETE FROM MenuItems WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddMenuItemAllergens(int menuItemId, List<int> allergenIds)
        {
            if (allergenIds == null || !allergenIds.Any())
                return;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var allergenId in allergenIds)
                {
                    using (SqlCommand command = new SqlCommand(@"
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
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
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var ingredient in ingredients)
                {
                    using (SqlCommand command = new SqlCommand(@"
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    DELETE FROM MenuItemIngredients WHERE MenuItemId = @MenuItemId", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddMenuItemModifiers(int menuItemId, List<int> modifierIds, Dictionary<int, decimal> modifierPrices)
        {
            if (modifierIds == null || !modifierIds.Any())
                return;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var modifierId in modifierIds)
                {
                    decimal priceAdjustment = 0;
                    if (modifierPrices != null && modifierPrices.ContainsKey(modifierId))
                    {
                        priceAdjustment = modifierPrices[modifierId];
                    }
                    
                    using (SqlCommand command = new SqlCommand(@"
                        INSERT INTO MenuItemModifiers (MenuItemId, ModifierId, PriceAdjustment, IsDefault)
                        VALUES (@MenuItemId, @ModifierId, @PriceAdjustment, @IsDefault)", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                        command.Parameters.AddWithValue("@ModifierId", modifierId);
                        command.Parameters.AddWithValue("@PriceAdjustment", priceAdjustment);
                        command.Parameters.AddWithValue("@IsDefault", false); // Default value
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void RemoveMenuItemModifiers(int menuItemId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    DELETE FROM MenuItemModifiers WHERE MenuItemId = @MenuItemId", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private Recipe GetRecipeByMenuItemId(int menuItemId)
        {
            Recipe recipe = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get recipe details
                using (SqlCommand command = new SqlCommand(@"
                    SELECT r.Id, r.Title, r.PreparationInstructions, r.CookingInstructions, 
                           r.PlatingInstructions, r.Yield, r.PreparationTimeMinutes, r.CookingTimeMinutes,
                           r.LastUpdated, r.CreatedById, r.Notes, r.IsArchived, r.Version
                    FROM Recipes r
                    WHERE r.MenuItemId = @MenuItemId", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            recipe = new Recipe
                            {
                                Id = reader.GetInt32(0),
                                MenuItemId = menuItemId,
                                Title = reader.GetString(1),
                                PreparationInstructions = reader.GetString(2),
                                CookingInstructions = reader.GetString(3),
                                PlatingInstructions = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Yield = reader.GetInt32(5),
                                PreparationTimeMinutes = reader.GetInt32(6),
                                CookingTimeMinutes = reader.GetInt32(7),
                                LastUpdated = reader.GetDateTime(8),
                                CreatedById = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                                Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                                IsArchived = reader.GetBoolean(11),
                                Version = reader.GetInt32(12),
                                Steps = new List<RecipeStep>()
                            };
                        }
                    }
                }
                
                if (recipe != null)
                {
                    // Get recipe steps
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT rs.Id, rs.StepNumber, rs.Description, rs.TimeRequiredMinutes, 
                               rs.Temperature, rs.SpecialEquipment, rs.Tips, rs.ImagePath
                        FROM RecipeSteps rs
                        WHERE rs.RecipeId = @RecipeId
                        ORDER BY rs.StepNumber", connection))
                    {
                        command.Parameters.AddWithValue("@RecipeId", recipe.Id);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                recipe.Steps.Add(new RecipeStep
                                {
                                    Id = reader.GetInt32(0),
                                    RecipeId = recipe.Id,
                                    StepNumber = reader.GetInt32(1),
                                    Description = reader.GetString(2),
                                    TimeRequiredMinutes = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                                    Temperature = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    SpecialEquipment = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Tips = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7)
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
            int recipeId;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    INSERT INTO Recipes (MenuItemId, Title, PreparationInstructions, CookingInstructions,
                                        PlatingInstructions, Yield, PreparationTimeMinutes, CookingTimeMinutes,
                                        LastUpdated, CreatedById, Notes, IsArchived, Version)
                    VALUES (@MenuItemId, @Title, @PreparationInstructions, @CookingInstructions,
                            @PlatingInstructions, @Yield, @PreparationTimeMinutes, @CookingTimeMinutes,
                            GETDATE(), @CreatedById, @Notes, @IsArchived, @Version);
                    SELECT SCOPE_IDENTITY();", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                    command.Parameters.AddWithValue("@Title", model.Title);
                    command.Parameters.AddWithValue("@PreparationInstructions", model.PreparationInstructions);
                    command.Parameters.AddWithValue("@CookingInstructions", model.CookingInstructions);
                    
                    if (!string.IsNullOrEmpty(model.PlatingInstructions))
                        command.Parameters.AddWithValue("@PlatingInstructions", model.PlatingInstructions);
                    else
                        command.Parameters.AddWithValue("@PlatingInstructions", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@Yield", model.Yield);
                    command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                    command.Parameters.AddWithValue("@CookingTimeMinutes", model.CookingTimeMinutes);
                    
                    // CreatedById is now an int instead of int?
                    command.Parameters.AddWithValue("@CreatedById", model.CreatedById);
                    
                    if (!string.IsNullOrEmpty(model.Notes))
                        command.Parameters.AddWithValue("@Notes", model.Notes);
                    else
                        command.Parameters.AddWithValue("@Notes", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsArchived", model.IsArchived);
                    command.Parameters.AddWithValue("@Version", model.Version);
                    
                    recipeId = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            
            return recipeId;
        }

        private void UpdateRecipe(RecipeViewModel model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    UPDATE Recipes
                    SET Title = @Title,
                        PreparationInstructions = @PreparationInstructions,
                        CookingInstructions = @CookingInstructions,
                        PlatingInstructions = @PlatingInstructions,
                        Yield = @Yield,
                        PreparationTimeMinutes = @PreparationTimeMinutes,
                        CookingTimeMinutes = @CookingTimeMinutes,
                        LastUpdated = GETDATE(),
                        Notes = @Notes,
                        IsArchived = @IsArchived,
                        Version = @Version
                    WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", model.Id);
                    command.Parameters.AddWithValue("@Title", model.Title);
                    command.Parameters.AddWithValue("@PreparationInstructions", model.PreparationInstructions);
                    command.Parameters.AddWithValue("@CookingInstructions", model.CookingInstructions);
                    
                    if (!string.IsNullOrEmpty(model.PlatingInstructions))
                        command.Parameters.AddWithValue("@PlatingInstructions", model.PlatingInstructions);
                    else
                        command.Parameters.AddWithValue("@PlatingInstructions", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@Yield", model.Yield);
                    command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                    command.Parameters.AddWithValue("@CookingTimeMinutes", model.CookingTimeMinutes);
                    
                    if (!string.IsNullOrEmpty(model.Notes))
                        command.Parameters.AddWithValue("@Notes", model.Notes);
                    else
                        command.Parameters.AddWithValue("@Notes", DBNull.Value);
                    
                    command.Parameters.AddWithValue("@IsArchived", model.IsArchived);
                    command.Parameters.AddWithValue("@Version", model.Version);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddRecipeSteps(int recipeId, List<RecipeStepViewModel> steps)
        {
            if (steps == null || !steps.Any())
                return;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                foreach (var step in steps)
                {
                    using (SqlCommand command = new SqlCommand(@"
                        INSERT INTO RecipeSteps (RecipeId, StepNumber, Description, TimeRequiredMinutes,
                                               Temperature, SpecialEquipment, Tips, ImagePath)
                        VALUES (@RecipeId, @StepNumber, @Description, @TimeRequiredMinutes,
                                @Temperature, @SpecialEquipment, @Tips, @ImagePath)", connection))
                    {
                        command.Parameters.AddWithValue("@RecipeId", recipeId);
                        command.Parameters.AddWithValue("@StepNumber", step.StepNumber);
                        command.Parameters.AddWithValue("@Description", step.Description);
                        
                        if (step.TimeRequiredMinutes.HasValue)
                            command.Parameters.AddWithValue("@TimeRequiredMinutes", step.TimeRequiredMinutes.Value);
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    DELETE FROM RecipeSteps WHERE RecipeId = @RecipeId", connection))
                {
                    command.Parameters.AddWithValue("@RecipeId", recipeId);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Helper methods for dropdown lists
        private List<SelectListItem> GetCategorySelectList()
        {
            var categories = new List<SelectListItem>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name 
                    FROM Categories
                    ORDER BY Name", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
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
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name, Description, IconPath
                    FROM Allergens
                    ORDER BY Name", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allergens.Add(new Allergen
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                IconPath = reader.IsDBNull(3) ? null : reader.GetString(3)
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
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, IngredientsName
                    FROM Ingredients
                    ORDER BY IngredientsName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ingredients.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
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
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name, Price, IsDefault
                    FROM Modifiers
                    ORDER BY Name", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            modifiers.Add(new Modifier
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                // Setting default values for the properties not in the database
                                Description = "Standard modifier",
                                ModifierType = "Addition",
                                IsActive = true
                            });
                        }
                    }
                }
            }
            
            return modifiers;
        }

        private List<SelectListItem> GetKitchenStationSelectList()
        {
            var kitchenStations = new List<SelectListItem>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name
                    FROM KitchenStations
                    ORDER BY Name", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            kitchenStations.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            
            return kitchenStations;
        }

        // Convert ViewModels to Models for ingredient list
        private List<Models.MenuItemIngredientViewModel> ConvertIngredientsViewModelToModel(List<ViewModels.MenuItemIngredientViewModel> viewModelIngredients)
        {
            var modelIngredients = new List<Models.MenuItemIngredientViewModel>();
            
            foreach (var ingredient in viewModelIngredients)
            {
                modelIngredients.Add(new Models.MenuItemIngredientViewModel
                {
                    IngredientId = ingredient.IngredientId,
                    Quantity = ingredient.Quantity,
                    Unit = ingredient.Unit,
                    IsOptional = ingredient.IsOptional,
                    Instructions = ingredient.Instructions
                });
            }
            
            return modelIngredients;
        }

        // Convert Models to ViewModels for ingredient list
        private ViewModels.MenuItemIngredientViewModel ConvertToViewModelMenuItemIngredient(Models.MenuItemIngredientViewModel modelIngredient)
        {
            return new ViewModels.MenuItemIngredientViewModel
            {
                IngredientId = modelIngredient.IngredientId,
                Quantity = modelIngredient.Quantity,
                Unit = modelIngredient.Unit,
                IsOptional = modelIngredient.IsOptional,
                Instructions = modelIngredient.Instructions
            };
        }
    }
}
