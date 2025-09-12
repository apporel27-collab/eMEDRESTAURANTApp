using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace RestaurantManagementSystem.Controllers
{
    public class DatabaseUpdateController : Controller
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DatabaseUpdateController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        // GET: DatabaseUpdate/AddCancelledAtColumn
        public IActionResult AddCancelledAtColumn()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Check if column exists
                    bool columnExists = false;
                    using (SqlCommand checkCommand = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Orders' 
                        AND COLUMN_NAME = 'CancelledAt'", connection))
                    {
                        int columnCount = Convert.ToInt32(checkCommand.ExecuteScalar());
                        columnExists = columnCount > 0;
                    }
                    
                    if (!columnExists)
                    {
                        // Add the column
                        using (SqlCommand alterCommand = new SqlCommand(@"
                            ALTER TABLE Orders
                            ADD CancelledAt DATETIME NULL", connection))
                        {
                            alterCommand.ExecuteNonQuery();
                            return Content("CancelledAt column added successfully to the Orders table.");
                        }
                    }
                    else
                    {
                        return Content("CancelledAt column already exists in the Orders table.");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Error adding CancelledAt column: {ex.Message}");
            }
        }
        
        // GET: DatabaseUpdate/CheckOrdersTableSchema
        public IActionResult CheckOrdersTableSchema()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'Orders'
                        ORDER BY ORDINAL_POSITION", connection))
                    {
                        var result = "<h3>Orders Table Schema</h3><table border='1'><tr><th>Column Name</th><th>Data Type</th><th>Max Length</th></tr>";
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader.GetString(0);
                                string dataType = reader.GetString(1);
                                string maxLength = reader.IsDBNull(2) ? "NULL" : reader.GetInt32(2).ToString();
                                
                                result += $"<tr><td>{columnName}</td><td>{dataType}</td><td>{maxLength}</td></tr>";
                            }
                        }
                        
                        result += "</table>";
                        return Content(result, "text/html");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Error checking Orders table schema: {ex.Message}");
            }
        }

        // GET: DatabaseUpdate/UpdateStoredProcedure
        public IActionResult UpdateStoredProcedure()
        {
            try
            {
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "update_sp_GetAllMenuItems.sql");
                string script = System.IO.File.ReadAllText(scriptPath);
                
                // Execute the script
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Split the script by GO statements
                    string[] batches = script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (string batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            using (var command = new SqlCommand(batch, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                
                return Content("Stored procedure updated successfully!");
            }
            catch (Exception ex)
            {
                return Content($"Error updating stored procedure: {ex.Message}");
            }
        }
    }
}
