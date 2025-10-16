using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Controllers
{
    public class MenuItemGroupController : Controller
    {
        private readonly RestaurantDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public MenuItemGroupController(RestaurantDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Ensure table exists in dbo schema (safe, idempotent)
        private void EnsureDboMenuItemGroupTableExists()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                var checkSql = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'menuitemgroup'";
                using var checkCmd = new SqlCommand(checkSql, conn);
                var exists = (int)checkCmd.ExecuteScalar() > 0;
                if (!exists)
                {
                    var createSql = @"
CREATE TABLE [dbo].[menuitemgroup](
    [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [itemgroup] [varchar](20) NULL,
    [is_active] [bit] NULL,
    [GST_Perc] [numeric](12, 2) NULL
) ON [PRIMARY]";
                    using var createCmd = new SqlCommand(createSql, conn);
                    createCmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Non-fatal: let EF surface errors if DB isn't available
            }
        }

        public IActionResult Index()
        {
            // Try to ensure table exists for older deployments where script wasn't run
            EnsureDboMenuItemGroupTableExists();

            var groups = _db.MenuItemGroups.OrderBy(g => g.ItemGroup).ToList();
            return View(groups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save([FromForm] MenuItemGroup model)
        {
            // Normalize boolean coming from checkbox (handle hidden false + checkbox true patterns and legacy 'on')
            try
            {
                if (Request.HasFormContentType && Request.Form.ContainsKey("IsActive"))
                {
                    var rawValues = Request.Form["IsActive"].ToArray();
                    bool parsed = model.IsActive; // fallback to bound value
                    if (rawValues.Length > 0)
                    {
                        var last = rawValues.Last();
                        if (rawValues.Length == 1)
                        {
                            parsed = last.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                                     last.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                     last == "1";
                        }
                        else
                        {
                            parsed = rawValues.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1" || v.Equals("on", StringComparison.OrdinalIgnoreCase));
                        }
                    }
                    model.IsActive = parsed;
                    if (ModelState.ContainsKey(nameof(MenuItemGroup.IsActive)))
                    {
                        ModelState[nameof(MenuItemGroup.IsActive)]!.Errors.Clear();
                    }
                }
            }
            catch { /* non-fatal */ }

            if (string.IsNullOrWhiteSpace(model.ItemGroup))
            {
                TempData["ErrorMessage"] = "Group name is required.";
                return RedirectToAction(nameof(Index));
            }

            if (model.ID == 0)
            {
                _db.MenuItemGroups.Add(model);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Menu item group created.";
            }
            else
            {
                var existing = _db.MenuItemGroups.FirstOrDefault(g => g.ID == model.ID);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Menu item group not found.";
                    return RedirectToAction(nameof(Index));
                }
                existing.ItemGroup = model.ItemGroup;
                existing.IsActive = model.IsActive;
                existing.GST_Perc = model.GST_Perc;
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Menu item group updated.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id)
        {
            var existing = _db.MenuItemGroups.FirstOrDefault(g => g.ID == id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Menu item group not found.";
            }
            else
            {
                existing.IsActive = !existing.IsActive;
                _db.SaveChanges();
                TempData["SuccessMessage"] = $"Menu item group {(existing.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
