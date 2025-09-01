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

        // GET: Recipe/Details/5
        public IActionResult Details(int id)
        {
            var recipe = GetRecipeById(id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // GET: Recipe/Edit/5
        public IActionResult Edit(int id)
        {
            var recipe = GetRecipeById(id);
            if (recipe == null)
            {
                return NotFound();
            }

            // Convert to view model
            var viewModel = new RecipeViewModel
            {
                Id = recipe.Id,
                MenuItemId = recipe.MenuItemId,
                MenuItemName = recipe.MenuItem?.Name ?? "Unknown Menu Item",
                Title = recipe.Title,
                PreparationInstructions = recipe.PreparationInstructions,
                CookingInstructions = recipe.CookingInstructions,
                PlatingInstructions = recipe.PlatingInstructions ?? "",
                Yield = recipe.Yield,
                PreparationTimeMinutes = recipe.PreparationTimeMinutes,
                CookingTimeMinutes = recipe.CookingTimeMinutes,
                Notes = recipe.Notes ?? "",
                IsArchived = recipe.IsArchived,
                Version = recipe.Version,
                Steps = recipe.Steps.OrderBy(s => s.StepNumber).Select(s => new RecipeStepViewModel
                {
                    Id = s.Id,
                    StepNumber = s.StepNumber,
                    Description = s.Description,
                    TimeRequiredMinutes = s.TimeRequiredMinutes,
                    Temperature = s.Temperature,
                    SpecialEquipment = s.SpecialEquipment,
                    Tips = s.Tips,
                    ImagePath = s.ImagePath
                }).ToList()
            };

            // If no steps, add one empty step
            if (!viewModel.Steps.Any())
            {
                viewModel.Steps.Add(new RecipeStepViewModel { StepNumber = 1 });
            }

            return View(viewModel);
        }

        // POST: Recipe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, RecipeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update recipe
                    UpdateRecipe(model);

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
                        RemoveRecipeSteps(id);

                        // Add updated steps
                        AddRecipeSteps(id, model.Steps);
                    }

                    TempData["SuccessMessage"] = "Recipe updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating recipe: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // Helper methods for database operations
        private List<Recipe> GetAllRecipes()
        {
            var recipes = new List<Recipe>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (SqlCommand command = new SqlCommand(@"
                    SELECT r.Id, r.MenuItemId, mi.Name AS MenuItemName, r.Title, 
                           r.PreparationTimeMinutes, r.CookingTimeMinutes, 
                           r.LastUpdated, r.IsArchived, r.Version
                    FROM Recipes r
                    JOIN MenuItems mi ON r.MenuItemId = mi.Id
                    ORDER BY r.Title", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recipes.Add(new Recipe
                            {
                                Id = reader.GetInt32(0),
                                MenuItemId = reader.GetInt32(1),
                                MenuItem = new MenuItem { Name = reader.GetString(2) },
                                Title = reader.GetString(3),
                                PreparationTimeMinutes = reader.GetInt32(4),
                                CookingTimeMinutes = reader.GetInt32(5),
                                LastUpdated = reader.GetDateTime(6),
                                IsArchived = reader.GetBoolean(7),
                                Version = reader.GetInt32(8)
                            });
                        }
                    }
                }
            }
            
            return recipes;
        }

        private Recipe GetRecipeById(int id)
        {
            Recipe recipe = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get recipe details
                using (SqlCommand command = new SqlCommand(@"
                    SELECT r.Id, r.MenuItemId, mi.Name AS MenuItemName, r.Title, 
                           r.PreparationInstructions, r.CookingInstructions, 
                           r.PlatingInstructions, r.Yield, r.PreparationTimeMinutes, 
                           r.CookingTimeMinutes, r.LastUpdated, r.CreatedById, 
                           r.Notes, r.IsArchived, r.Version
                    FROM Recipes r
                    JOIN MenuItems mi ON r.MenuItemId = mi.Id
                    WHERE r.Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            recipe = new Recipe
                            {
                                Id = reader.GetInt32(0),
                                MenuItemId = reader.GetInt32(1),
                                MenuItem = new MenuItem { Name = reader.GetString(2) },
                                Title = reader.GetString(3),
                                PreparationInstructions = reader.GetString(4),
                                CookingInstructions = reader.GetString(5),
                                PlatingInstructions = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Yield = reader.GetInt32(7),
                                PreparationTimeMinutes = reader.GetInt32(8),
                                CookingTimeMinutes = reader.GetInt32(9),
                                LastUpdated = reader.GetDateTime(10),
                                CreatedById = reader.IsDBNull(11) ? null : (int?)reader.GetInt32(11),
                                Notes = reader.IsDBNull(12) ? null : reader.GetString(12),
                                IsArchived = reader.GetBoolean(13),
                                Version = reader.GetInt32(14),
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
    }
}
