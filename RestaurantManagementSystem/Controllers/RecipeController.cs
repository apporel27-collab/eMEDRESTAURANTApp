using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Choose Microsoft.Data.SqlClient explicitly
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using MenuItemIngredientViewModelModel = RestaurantManagementSystem.Models.MenuItemIngredientViewModel;

namespace RestaurantManagementSystem.Controllers
{
    public class RecipeController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RecipeController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _webHostEnvironment = webHostEnvironment;
        }
        
        // GET: Recipe
        public IActionResult Index()
        {
            var recipes = GetAllRecipes();
            return View(recipes);
        }
        
        // GET: Recipe/Dashboard
        public IActionResult Dashboard()
        {
            var recipes = GetAllRecipes();
            return View(recipes);
        }

        // GET: Recipe/Details/5
        public IActionResult Details(int id)
        {
            var recipe = GetRecipeByMenuItemId(id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // GET: Recipe/Create?menuItemId=5 (legacy) => redirect to Menu/Recipe/5
        public IActionResult Create(int menuItemId)
        {
            // Preserve existing deep-link compatibility but serve from MenuController.Recipe
            return RedirectToAction("Recipe", "Menu", new { id = menuItemId });
        }

        // POST: Recipe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RecipeViewModel model)
        {
            if (ModelState.IsValid)
            {
                int recipeId;
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (SqlCommand command = new SqlCommand("sp_ManageRecipe", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                        command.Parameters.AddWithValue("@Title", model.Title);
                        command.Parameters.AddWithValue("@PreparationInstructions", model.PreparationInstructions);
                        command.Parameters.AddWithValue("@CookingInstructions", model.CookingInstructions);
                        command.Parameters.AddWithValue("@PlatingInstructions", model.PlatingInstructions ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Yield", model.Yield);
                        command.Parameters.AddWithValue("@YieldPercentage", model.YieldPercentage);
                        command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                        command.Parameters.AddWithValue("@CookingTimeMinutes", model.CookingTimeMinutes);
                        command.Parameters.AddWithValue("@Notes", model.Notes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UserId", 1); // TODO: Get from authentication
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                recipeId = reader.GetInt32(reader.GetOrdinal("RecipeId"));
                            }
                            else
                            {
                                ModelState.AddModelError("", "Failed to create recipe");
                                return View(model);
                            }
                        }
                    }
                    
                    // Add recipe steps if provided
                    if (model.Steps != null && model.Steps.Count > 0)
                    {
                        for (int i = 0; i < model.Steps.Count; i++)
                        {
                            var step = model.Steps[i];
                            
                            using (SqlCommand command = new SqlCommand("sp_ManageRecipeStep", connection))
                            {
                                command.CommandType = System.Data.CommandType.StoredProcedure;
                                
                                command.Parameters.AddWithValue("@RecipeId", recipeId);
                                command.Parameters.AddWithValue("@StepNumber", i + 1);
                                command.Parameters.AddWithValue("@Description", step.Description);
                                command.Parameters.AddWithValue("@TimeRequiredMinutes", step.TimeRequiredMinutes ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Temperature", step.Temperature ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@SpecialEquipment", step.SpecialEquipment ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Tips", step.Tips ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@ImagePath", step.ImagePath ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@IsUpdate", false);
                                
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    // Add ingredients if provided
                    if (model.Ingredients != null && model.Ingredients.Count > 0)
                    {
                        foreach (var ingredient in model.Ingredients)
                        {
                            using (SqlCommand command = new SqlCommand("sp_ManageMenuItemIngredient", connection))
                            {
                                command.CommandType = System.Data.CommandType.StoredProcedure;
                                
                                command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                                command.Parameters.AddWithValue("@IngredientId", ingredient.IngredientId);
                                command.Parameters.AddWithValue("@Quantity", ingredient.Quantity);
                                command.Parameters.AddWithValue("@Unit", ingredient.Unit);
                                command.Parameters.AddWithValue("@IsOptional", ingredient.IsOptional);
                                command.Parameters.AddWithValue("@Instructions", ingredient.Instructions ?? (object)DBNull.Value);
                                
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                
                TempData["SuccessMessage"] = "Recipe created successfully";
                return RedirectToAction("Details", "Menu", new { id = model.MenuItemId });
            }
            
            ViewBag.Ingredients = new SelectList(GetAllIngredients(), "Id", "IngredientsName");
            return View(model);
        }
        
        // GET: Recipe/Edit/5 (where 5 is the MenuItemId)
        public IActionResult Edit(int menuItemId)
        {
            var recipe = GetRecipeByMenuItemId(menuItemId);
            if (recipe == null)
            {
                return RedirectToAction("Create", new { menuItemId });
            }
            
            var menuItem = GetMenuItemById(menuItemId);
            ViewBag.MenuItem = menuItem;
            ViewBag.Ingredients = new SelectList(GetAllIngredients(), "Id", "IngredientsName");
            
            return View(recipe);
        }
        
        // POST: Recipe/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RecipeViewModel model)
        {
            if (ModelState.IsValid)
            {
                int recipeId;
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (SqlCommand command = new SqlCommand("sp_ManageRecipe", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                        command.Parameters.AddWithValue("@Title", model.Title);
                        command.Parameters.AddWithValue("@PreparationInstructions", model.PreparationInstructions);
                        command.Parameters.AddWithValue("@CookingInstructions", model.CookingInstructions);
                        command.Parameters.AddWithValue("@PlatingInstructions", model.PlatingInstructions ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Yield", model.Yield);
                        command.Parameters.AddWithValue("@YieldPercentage", model.YieldPercentage);
                        command.Parameters.AddWithValue("@PreparationTimeMinutes", model.PreparationTimeMinutes);
                        command.Parameters.AddWithValue("@CookingTimeMinutes", model.CookingTimeMinutes);
                        command.Parameters.AddWithValue("@Notes", model.Notes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UserId", 1); // TODO: Get from authentication
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                recipeId = reader.GetInt32(reader.GetOrdinal("RecipeId"));
                            }
                            else
                            {
                                ModelState.AddModelError("", "Failed to update recipe");
                                return View(model);
                            }
                        }
                    }
                    
                    // Update recipe steps
                    if (model.Steps != null && model.Steps.Count > 0)
                    {
                        for (int i = 0; i < model.Steps.Count; i++)
                        {
                            var step = model.Steps[i];
                            
                            using (SqlCommand command = new SqlCommand("sp_ManageRecipeStep", connection))
                            {
                                command.CommandType = System.Data.CommandType.StoredProcedure;
                                
                                command.Parameters.AddWithValue("@RecipeId", recipeId);
                                command.Parameters.AddWithValue("@StepNumber", i + 1);
                                command.Parameters.AddWithValue("@Description", step.Description);
                                command.Parameters.AddWithValue("@TimeRequiredMinutes", step.TimeRequiredMinutes ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Temperature", step.Temperature ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@SpecialEquipment", step.SpecialEquipment ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Tips", step.Tips ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@ImagePath", step.ImagePath ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@IsUpdate", true);
                                
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    // Update ingredients
                    // First, remove all existing ingredients
                    using (SqlCommand command = new SqlCommand("DELETE FROM MenuItemIngredients WHERE MenuItemId = @MenuItemId", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                        command.ExecuteNonQuery();
                    }
                    
                    // Add new ingredients
                    if (model.Ingredients != null && model.Ingredients.Count > 0)
                    {
                        foreach (var ingredient in model.Ingredients)
                        {
                            using (SqlCommand command = new SqlCommand("sp_ManageMenuItemIngredient", connection))
                            {
                                command.CommandType = System.Data.CommandType.StoredProcedure;
                                
                                command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                                command.Parameters.AddWithValue("@IngredientId", ingredient.IngredientId);
                                command.Parameters.AddWithValue("@Quantity", ingredient.Quantity);
                                command.Parameters.AddWithValue("@Unit", ingredient.Unit);
                                command.Parameters.AddWithValue("@IsOptional", ingredient.IsOptional);
                                command.Parameters.AddWithValue("@Instructions", ingredient.Instructions ?? (object)DBNull.Value);
                                
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                
                TempData["SuccessMessage"] = "Recipe updated successfully";
                return RedirectToAction("Details", "Menu", new { id = model.MenuItemId });
            }
            
            ViewBag.Ingredients = new SelectList(GetAllIngredients(), "Id", "IngredientsName");
            return View(model);
        }
        
        // GET: Recipe/CalculateSuggestedPrice/5?targetGP=40
        public IActionResult CalculateSuggestedPrice(int id, decimal targetGP = 40)
        {
            var menuItem = GetMenuItemById(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            
            PriceSuggestionViewModel priceViewModel = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand("sp_CalculateSuggestedPrice", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    
                    command.Parameters.AddWithValue("@MenuItemId", id);
                    command.Parameters.AddWithValue("@TargetGPPercentage", targetGP);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            priceViewModel = new PriceSuggestionViewModel
                            {
                                MenuItemId = reader.GetInt32(reader.GetOrdinal("MenuItemId")),
                                TotalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost")),
                                TargetGPPercentage = reader.GetDecimal(reader.GetOrdinal("TargetGPPercentage")),
                                SuggestedPrice = reader.GetDecimal(reader.GetOrdinal("SuggestedPrice")),
                                CurrentPrice = menuItem.Price
                            };
                        }
                    }
                }
            }
            
            if (priceViewModel == null)
            {
                priceViewModel = new PriceSuggestionViewModel
                {
                    MenuItemId = id,
                    TotalCost = 0,
                    TargetGPPercentage = targetGP,
                    SuggestedPrice = 0,
                    CurrentPrice = menuItem.Price
                };
            }
            
            priceViewModel.MenuItemName = menuItem.Name;
            
            return View(priceViewModel);
        }
        
        // POST: Recipe/UpdatePrice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePrice(PriceSuggestionViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (SqlCommand command = new SqlCommand("sp_UpdateMenuItem", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        
                        // Get current menu item data
                        var menuItem = GetMenuItemById(model.MenuItemId);
                        
                        command.Parameters.AddWithValue("@Id", model.MenuItemId);
                        command.Parameters.AddWithValue("@PLUCode", menuItem.PLUCode);
                        command.Parameters.AddWithValue("@Name", menuItem.Name);
                        command.Parameters.AddWithValue("@Description", menuItem.Description);
                        command.Parameters.AddWithValue("@Price", model.NewPrice);
                        command.Parameters.AddWithValue("@CategoryId", menuItem.CategoryId);
                        command.Parameters.AddWithValue("@ImagePath", menuItem.ImagePath ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsAvailable", menuItem.IsAvailable);
                        command.Parameters.AddWithValue("@PreparationTimeMinutes", menuItem.PreparationTimeMinutes);
                        command.Parameters.AddWithValue("@CalorieCount", menuItem.CalorieCount ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsFeatured", menuItem.IsFeatured);
                        command.Parameters.AddWithValue("@IsSpecial", menuItem.IsSpecial);
                        command.Parameters.AddWithValue("@DiscountPercentage", menuItem.DiscountPercentage ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@KitchenStationId", menuItem.KitchenStationId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TargetGP", model.TargetGPPercentage);
                        command.Parameters.AddWithValue("@UserId", 1); // TODO: Get from authentication
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool needsApproval = reader.GetBoolean(reader.GetOrdinal("NeedsPriceApproval"));
                                if (needsApproval)
                                {
                                    TempData["WarningMessage"] = "Price change requires approval. The current price will remain until approved.";
                                }
                                else
                                {
                                    TempData["SuccessMessage"] = "Price updated successfully.";
                                }
                            }
                        }
                    }
                }
                
                return RedirectToAction("Details", "Menu", new { id = model.MenuItemId });
            }
            
            return View("CalculateSuggestedPrice", model);
        }
        
        // Helper methods
        private RecipeViewModel GetRecipeByMenuItemId(int menuItemId)
        {
            RecipeViewModel recipe = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand("sp_GetRecipeByMenuItemId", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            recipe = new RecipeViewModel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                MenuItemId = reader.GetInt32(reader.GetOrdinal("MenuItemId")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                PreparationInstructions = reader.GetString(reader.GetOrdinal("PreparationInstructions")),
                                CookingInstructions = reader.GetString(reader.GetOrdinal("CookingInstructions")),
                                PlatingInstructions = reader.IsDBNull(reader.GetOrdinal("PlatingInstructions")) ? null : reader.GetString(reader.GetOrdinal("PlatingInstructions")),
                                Yield = reader.GetInt32(reader.GetOrdinal("Yield")),
                                YieldPercentage = reader.GetDecimal(reader.GetOrdinal("YieldPercentage")),
                                PreparationTimeMinutes = reader.GetInt32(reader.GetOrdinal("PreparationTimeMinutes")),
                                CookingTimeMinutes = reader.GetInt32(reader.GetOrdinal("CookingTimeMinutes")),
                                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                                Version = reader.GetInt32(reader.GetOrdinal("Version")),
                                Steps = new List<RecipeStepViewModel>(),
                                Ingredients = new List<MenuItemIngredientViewModelModel>()
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                    
                    // Get recipe steps
                    using (SqlCommand stepsCommand = new SqlCommand("sp_GetRecipeStepsByRecipeId", connection))
                    {
                        stepsCommand.CommandType = System.Data.CommandType.StoredProcedure;
                        stepsCommand.Parameters.AddWithValue("@RecipeId", recipe.Id);
                        
                        using (SqlDataReader stepsReader = stepsCommand.ExecuteReader())
                        {
                            while (stepsReader.Read())
                            {
                                recipe.Steps.Add(new RecipeStepViewModel
                                {
                                    Id = stepsReader.GetInt32(stepsReader.GetOrdinal("Id")),
                                    RecipeId = stepsReader.GetInt32(stepsReader.GetOrdinal("RecipeId")),
                                    StepNumber = stepsReader.GetInt32(stepsReader.GetOrdinal("StepNumber")),
                                    Description = stepsReader.GetString(stepsReader.GetOrdinal("Description")),
                                    TimeRequiredMinutes = stepsReader.IsDBNull(stepsReader.GetOrdinal("TimeRequiredMinutes")) ? null : (int?)stepsReader.GetInt32(stepsReader.GetOrdinal("TimeRequiredMinutes")),
                                    Temperature = stepsReader.IsDBNull(stepsReader.GetOrdinal("Temperature")) ? null : stepsReader.GetString(stepsReader.GetOrdinal("Temperature")),
                                    SpecialEquipment = stepsReader.IsDBNull(stepsReader.GetOrdinal("SpecialEquipment")) ? null : stepsReader.GetString(stepsReader.GetOrdinal("SpecialEquipment")),
                                    Tips = stepsReader.IsDBNull(stepsReader.GetOrdinal("Tips")) ? null : stepsReader.GetString(stepsReader.GetOrdinal("Tips")),
                                    ImagePath = stepsReader.IsDBNull(stepsReader.GetOrdinal("ImagePath")) ? null : stepsReader.GetString(stepsReader.GetOrdinal("ImagePath"))
                                });
                            }
                        }
                    }
                    
                    // Get ingredients
                    using (SqlCommand ingredientsCommand = new SqlCommand("sp_GetMenuItemIngredientsByMenuItemId", connection))
                    {
                        ingredientsCommand.CommandType = System.Data.CommandType.StoredProcedure;
                        ingredientsCommand.Parameters.AddWithValue("@MenuItemId", recipe.MenuItemId);
                        
                        using (SqlDataReader ingredientsReader = ingredientsCommand.ExecuteReader())
                        {
                            while (ingredientsReader.Read())
                            {
                                recipe.Ingredients.Add(new MenuItemIngredientViewModelModel
                                {
                                    Id = ingredientsReader.GetInt32(ingredientsReader.GetOrdinal("Id")),
                                    MenuItemId = ingredientsReader.GetInt32(ingredientsReader.GetOrdinal("MenuItemId")),
                                    IngredientId = ingredientsReader.GetInt32(ingredientsReader.GetOrdinal("IngredientId")),
                                    IngredientName = ingredientsReader.GetString(ingredientsReader.GetOrdinal("IngredientsName")),
                                    Quantity = ingredientsReader.GetDecimal(ingredientsReader.GetOrdinal("Quantity")),
                                    Unit = ingredientsReader.GetString(ingredientsReader.GetOrdinal("Unit")),
                                    IsOptional = ingredientsReader.GetBoolean(ingredientsReader.GetOrdinal("IsOptional"))
                                });
                            }
                        }
                    }
                }
            }
            return recipe;
        }
        
        private MenuItem GetMenuItemById(int id)
        {
            MenuItem menuItem = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
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
                                KitchenStationId = null // Default value
                            };
                        }
                    }
                }
            }
            
            return menuItem;
        }
        
        private List<Ingredients> GetAllIngredients()
        {
            var ingredients = new List<Ingredients>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand("SELECT Id, IngredientsName, DisplayName, Code FROM Ingredients ORDER BY IngredientsName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ingredients.Add(new Ingredients
                            {
                                Id = reader.GetInt32(0),
                                IngredientsName = reader.GetString(1),
                                DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Code = reader.IsDBNull(3) ? null : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            
            return ingredients;
        }
        
        // Helper method to execute SQL script files
        private void ExecuteScriptFile(string fileName)
        {
            try
            {
                string scriptPath = Path.Combine(_webHostEnvironment.ContentRootPath, "SQL", fileName);
                if (System.IO.File.Exists(scriptPath))
                {
                    string script = System.IO.File.ReadAllText(scriptPath);
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        // Split the script by GO statements if needed
                        foreach (string batch in script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!string.IsNullOrWhiteSpace(batch))
                            {
                                using (SqlCommand command = new SqlCommand(batch, connection))
                                {
                                    try
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error executing script {fileName}: {ex.Message}");
                                        // Continue with next batch even if this one fails
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Script file not found: {scriptPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExecuteScriptFile: {ex.Message}");
            }
        }

        private List<Recipe> GetAllRecipes()
        {
            List<Recipe> recipes = new List<Recipe>();
            
            try
            {
                // First, ensure stored procedures are available
                ExecuteScriptFile("create_sp_GetAllRecipes.sql");
                ExecuteScriptFile("update_sp_GetAllRecipes.sql");
                
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (SqlCommand command = new SqlCommand("sp_GetAllRecipes", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                recipes.Add(new Recipe
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    MenuItemId = reader.GetInt32(reader.GetOrdinal("MenuItemId")),
                                    Title = reader.GetString(reader.GetOrdinal("Title")),
                                    PreparationInstructions = reader.IsDBNull(reader.GetOrdinal("PreparationInstructions")) ? null : reader.GetString(reader.GetOrdinal("PreparationInstructions")),
                                    CookingInstructions = reader.IsDBNull(reader.GetOrdinal("CookingInstructions")) ? null : reader.GetString(reader.GetOrdinal("CookingInstructions")),
                                    PlatingInstructions = reader.IsDBNull(reader.GetOrdinal("PlatingInstructions")) ? null : reader.GetString(reader.GetOrdinal("PlatingInstructions")),
                                    Yield = reader.GetInt32(reader.GetOrdinal("Yield")),
                                    YieldPercentage = reader.GetDecimal(reader.GetOrdinal("YieldPercentage")),
                                    PreparationTimeMinutes = reader.GetInt32(reader.GetOrdinal("PreparationTimeMinutes")),
                                    CookingTimeMinutes = reader.GetInt32(reader.GetOrdinal("CookingTimeMinutes")),
                                    LastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated")),
                                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                                    IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                                    Version = reader.GetInt32(reader.GetOrdinal("Version")),
                                    MenuItemName = reader.GetString(reader.GetOrdinal("MenuItemName"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving recipes: {ex.Message}");
                // Log the error or display it
            }
            
            return recipes;
        }
    }
}
