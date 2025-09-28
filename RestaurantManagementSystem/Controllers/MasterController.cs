using Microsoft.AspNetCore.Mvc;
using RestaurantManagementSystem.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;

namespace RestaurantManagementSystem.Controllers
{
    public class MasterController : Controller
    {
        private readonly RestaurantDbContext _dbContext;
        
        public MasterController(RestaurantDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Category List
        public IActionResult CategoryList()
        {
            var categories = _dbContext.Categories.ToList();
            return View(categories);
    }

    // Category Add/Edit/View Form
    public IActionResult CategoryForm(int? id, bool isView = false)
    {
        Category model = new Category { Name = "" };
        if (id.HasValue)
        {
            model = _dbContext.Categories.FirstOrDefault(c => c.Id == id.Value) ?? model;
        }
        
        ViewBag.IsView = isView;
        return View(model);
    }

    [HttpPostAttribute]
    public IActionResult CategoryForm(Category model)
    {
        string resultMessage;
        
        if (model.Id > 0)
        {
            // Update existing category
            var existingCategory = _dbContext.Categories.FirstOrDefault(c => c.Id == model.Id);
            if (existingCategory == null)
            {
                TempData["ResultMessage"] = "Category update failed. Id not found.";
                return RedirectToAction("CategoryList");
            }
            
            existingCategory.Name = model.Name;
            existingCategory.IsActive = model.IsActive;
            _dbContext.SaveChanges();
            resultMessage = "Category updated successfully.";
        }
        else
        {
            // Add new category
            _dbContext.Categories.Add(model);
            _dbContext.SaveChanges();
            resultMessage = "Category added successfully.";
        }
        
        TempData["ResultMessage"] = resultMessage;
        return RedirectToAction("CategoryList");
    }

    // Ingredients List
    public IActionResult IngredientsList()
    {
        var ingredients = _dbContext.Ingredients.ToList();
        
        // If there are no ingredients, seed some sample data
        if (!ingredients.Any())
        {
            _dbContext.Ingredients.AddRange(
                new Ingredients { IngredientsName = "Tomato", DisplayName = "Tomato", Code = "TMT" },
                new Ingredients { IngredientsName = "Cheese", DisplayName = "Cheese", Code = "CHS" }
            );
            _dbContext.SaveChanges();
            ingredients = _dbContext.Ingredients.ToList();
        }
        
        return View(ingredients);
    }

    // Add/Edit/View Form
    public IActionResult IngredientsForm(int? id, bool isView = false)
    {
        Ingredients model = new Ingredients { IngredientsName = "" };
        
        if (id.HasValue)
        {
            model = _dbContext.Ingredients.FirstOrDefault(i => i.Id == id.Value) ?? model;
        }
        
        ViewBag.IsView = isView;
        return View("Ingredients", model);
    }

    [HttpPostAttribute]
    public IActionResult IngredientsForm(Ingredients model)
    {
        if (ModelState.IsValid)
        {
            if (model.Id == 0)
            {
                // Add new ingredient
                _dbContext.Ingredients.Add(model);
                _dbContext.SaveChanges();
                TempData["ResultMessage"] = "Ingredient added successfully.";
            }
            else
            {
                // Update existing ingredient
                var existingIngredient = _dbContext.Ingredients.FirstOrDefault(i => i.Id == model.Id);
                if (existingIngredient != null)
                {
                    existingIngredient.IngredientsName = model.IngredientsName;
                    existingIngredient.DisplayName = model.DisplayName;
                    existingIngredient.Code = model.Code;
                    _dbContext.SaveChanges();
                    TempData["ResultMessage"] = "Ingredient updated successfully.";
                }
                else
                {
                    TempData["ResultMessage"] = "Ingredient update failed. Id not found.";
                }
            }
            return RedirectToAction("IngredientsList");
        }
        return View("Ingredients", model);
    }
}
}