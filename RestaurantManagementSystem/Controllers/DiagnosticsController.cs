using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace RestaurantManagementSystem.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly string _connectionString;

        public DiagnosticsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Diagnostics/CheckTable
        public IActionResult CheckTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return Content("Please provide a table name");
            }

            var result = new Dictionary<string, object>();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Check if table exists
                    using (var command = new SqlCommand(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName",
                        connection))
                    {
                        command.Parameters.AddWithValue("@TableName", tableName);
                        int tableExists = (int)command.ExecuteScalar();
                        result.Add("TableExists", tableExists > 0);
                        
                        if (tableExists > 0)
                        {
                            // Get column info
                            using (var columnsCommand = new SqlCommand(
                                "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName",
                                connection))
                            {
                                columnsCommand.Parameters.AddWithValue("@TableName", tableName);
                                var columns = new List<object>();
                                
                                using (var reader = columnsCommand.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        columns.Add(new
                                        {
                                            Name = reader.GetString(0),
                                            Type = reader.GetString(1),
                                            IsNullable = reader.GetString(2)
                                        });
                                    }
                                }
                                
                                result.Add("Columns", columns);
                            }
                            
                            // Get row count
                            using (var countCommand = new SqlCommand(
                                $"SELECT COUNT(*) FROM {tableName}",
                                connection))
                            {
                                int rowCount = (int)countCommand.ExecuteScalar();
                                result.Add("RowCount", rowCount);
                                
                                // Get sample data (first 10 rows)
                                if (rowCount > 0)
                                {
                                    using (var dataCommand = new SqlCommand(
                                        $"SELECT TOP 10 * FROM {tableName}",
                                        connection))
                                    {
                                        var rows = new List<Dictionary<string, object>>();
                                        
                                        using (var reader = dataCommand.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                var row = new Dictionary<string, object>();
                                                
                                                for (int i = 0; i < reader.FieldCount; i++)
                                                {
                                                    string columnName = reader.GetName(i);
                                                    object value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i);
                                                    row.Add(columnName, value);
                                                }
                                                
                                                rows.Add(row);
                                            }
                                        }
                                        
                                        result.Add("SampleData", rows);
                                    }
                                }
                            }
                        }
                    }
                }
                
                return Json(result);
            }
            catch (Exception ex)
            {
                result.Add("Error", ex.Message);
                if (ex.InnerException != null)
                {
                    result.Add("InnerError", ex.InnerException.Message);
                }
                
                return Json(result);
            }
        }
    }
}
