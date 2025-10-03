using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.IO;

namespace RestaurantManagementSystem.Controllers
{
    public class DatabaseFixV2Controller : Controller
    {
        private readonly string _connectionString;

        public DatabaseFixV2Controller(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: DatabaseFixV2/FixMenuItemsTable
        public IActionResult FixMenuItemsTable()
        {
            try
            {
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "fix_menuitems_table.sql");
                string script = System.IO.File.ReadAllText(scriptPath);
                
                // Execute the script
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(script, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                
                return Content("MenuItems table fixed successfully!");
            }
            catch (Exception ex)
            {
                return Content($"Error fixing MenuItems table: {ex.Message}");
            }
        }
    }
}
