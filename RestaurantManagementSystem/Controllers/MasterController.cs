using Microsoft.AspNetCore.Mvc;
using RestaurantManagementSystem.Models;
using System.Collections.Generic;

public class MasterController : Controller
{
    // Category List
    public IActionResult CategoryList()
    {
        // Replace with actual DB fetch. For now, use sample data:
        var categories = new List<Category>
        {
            new Category { Id = 1, CategoryName = "Vegetarian", IsActive = true },
            new Category { Id = 2, CategoryName = "Non-Vegetarian", IsActive = false }
        };
        return View(categories);
    }

    // Category Add/Edit/View Form
    public IActionResult CategoryForm(int? id, bool isView = false)
    {
        Category model = id.HasValue
            ? new Category { Id = id.Value, CategoryName = "Sample", IsActive = true }
            : new Category { CategoryName = "" };
        ViewBag.IsView = isView;
        return View(model);
    }

    [HttpPost]
    public IActionResult CategoryForm(Category model)
    {
        if (ModelState.IsValid)
        {
            if (model.Id == 0)
            {
                // TODO: Add new category
            }
            else
            {
                // TODO: Update existing category
            }
            return RedirectToAction("CategoryList");
        }
        return View(model);
    }

    // Ingredients List
    public IActionResult IngredientsList()
    {
        var ingredients = new List<Ingredients>
        {
            new Ingredients { Id = 1, IngredientsName = "Tomato", DisplayName = "Tomato", Code = "TMT" },
            new Ingredients { Id = 2, IngredientsName = "Cheese", DisplayName = "Cheese", Code = "CHS" }
        };
        return View(ingredients);
    }

    // Add/Edit/View Form
    public IActionResult IngredientsForm(int? id, bool isView = false)
    {
        Ingredients model = id.HasValue
            ? new Ingredients { Id = id.Value, IngredientsName = "Sample", DisplayName = "Sample", Code = "SMP" }
            : new Ingredients { IngredientsName = "" };
        ViewBag.IsView = isView;
        return View("Ingredients", model);
    }

    [HttpPost]
    public IActionResult IngredientsForm(Ingredients model)
    {
        if (ModelState.IsValid)
        {
            if (model.Id == 0)
            {
                // TODO: Add new ingredient
            }
            else
            {
                // TODO: Update existing ingredient
            }
            return RedirectToAction("IngredientsList");
        }
        return View("Ingredients", model);
    }
}