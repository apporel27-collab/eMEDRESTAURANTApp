using Microsoft.AspNetCore.Mvc;
using RestaurantManagementSystem.Models;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

public class MasterController : Controller
{
    private readonly IConfiguration _config;
    public MasterController(IConfiguration config)
    {
        _config = config;
    }

    // Category List
    public IActionResult CategoryList()
    {
        var categories = new List<Category>();
        using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            con.Open();
            using (var cmd = new SqlCommand("SELECT Id, CategoryName, IsActive FROM Category", con))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            Id = reader.GetInt32(0),
                            CategoryName = reader.IsDBNull(1) ? "" : reader.GetString(1), // Fix warning
                            IsActive = reader.GetBoolean(2)
                        });
                    }
                }
            }
        }
        return View(categories);
    }

    // Category Add/Edit/View Form
    public IActionResult CategoryForm(int? id, bool isView = false)
    {
        Category model = new Category { CategoryName = "" };
        if (id.HasValue)
        {
            using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (var cmd = new SqlCommand("SELECT Id, CategoryName, IsActive FROM Category WHERE Id = @Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Id = reader.GetInt32(0);
                            model.CategoryName = reader.IsDBNull(1) ? "" : reader.GetString(1); // Fix warning
                            model.IsActive = reader.GetBoolean(2);
                        }
                    }
                }
            }
        }
        ViewBag.IsView = isView;
        return View(model);
    }

    [HttpPost]
    public IActionResult CategoryForm(Category model)
    {
        string resultMessage = "";
        using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            con.Open();
            // Check if updating and Id exists
            if (model.Id > 0)
            {
                using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Category WHERE Id = @Id", con))
                {
                    checkCmd.Parameters.AddWithValue("@Id", model.Id);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count == 0)
                    {
                        TempData["ResultMessage"] = "Category update failed. Id not found.";
                        return RedirectToAction("CategoryList");
                    }
                }
            }
            using (var cmd = new SqlCommand("usp_UpsertCategory", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", model.Id == 0 ? 0 : model.Id);
                cmd.Parameters.AddWithValue("@CategoryName", model.CategoryName);
                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        resultMessage = reader["Message"].ToString();
                    }
                }
            }
        }
        TempData["ResultMessage"] = resultMessage;
        return RedirectToAction("CategoryList");
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