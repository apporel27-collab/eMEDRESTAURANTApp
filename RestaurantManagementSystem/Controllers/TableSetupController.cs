using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace RestaurantManagementSystem.Controllers
{
    // Add this method to your HomeController or run it as a standalone script
    public class TableSetupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        
        public TableSetupController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        public IActionResult CreateOrderItemModifiersTables()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Check if OrderItemModifiers table exists
                    bool orderItemModifiersExists = false;
                    using (SqlCommand checkCommand = new SqlCommand(
                        "SELECT CASE WHEN OBJECT_ID('OrderItemModifiers', 'U') IS NOT NULL THEN 1 ELSE 0 END", 
                        connection))
                    {
                        orderItemModifiersExists = Convert.ToBoolean(checkCommand.ExecuteScalar());
                    }
                    
                    // Check if OrderItem_Modifiers table exists
                    bool orderItemModifiersWithUnderscoreExists = false;
                    using (SqlCommand checkCommand = new SqlCommand(
                        "SELECT CASE WHEN OBJECT_ID('OrderItem_Modifiers', 'U') IS NOT NULL THEN 1 ELSE 0 END", 
                        connection))
                    {
                        orderItemModifiersWithUnderscoreExists = Convert.ToBoolean(checkCommand.ExecuteScalar());
                    }
                    
                    // Create OrderItemModifiers table if it doesn't exist
                    if (!orderItemModifiersExists)
                    {
                        using (SqlCommand createCommand = new SqlCommand(@"
                            CREATE TABLE [dbo].[OrderItemModifiers] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [OrderItemId] INT NOT NULL,
                                [ModifierId] INT NOT NULL,
                                [Price] DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
                                [Quantity] INT NOT NULL DEFAULT 1,
                                [Subtotal] DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
                                [Notes] NVARCHAR(255) NULL,
                                [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                                [UpdatedAt] DATETIME NULL
                            )", connection))
                        {
                            createCommand.ExecuteNonQuery();
                            ViewBag.Message = "Created OrderItemModifiers table. ";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "OrderItemModifiers table already exists. ";
                    }
                    
                    // Create OrderItem_Modifiers table if it doesn't exist
                    if (!orderItemModifiersWithUnderscoreExists)
                    {
                        using (SqlCommand createCommand = new SqlCommand(@"
                            CREATE TABLE [dbo].[OrderItem_Modifiers] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [OrderItemId] INT NOT NULL,
                                [ModifierId] INT NOT NULL,
                                [Price] DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
                                [Quantity] INT NOT NULL DEFAULT 1,
                                [Subtotal] DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
                                [Notes] NVARCHAR(255) NULL,
                                [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                                [UpdatedAt] DATETIME NULL
                            )", connection))
                        {
                            createCommand.ExecuteNonQuery();
                            ViewBag.Message += "Created OrderItem_Modifiers table.";
                        }
                    }
                    else
                    {
                        ViewBag.Message += "OrderItem_Modifiers table already exists.";
                    }
                }
                
                return Content($"Tables setup completed: {ViewBag.Message}");
            }
            catch (Exception ex)
            {
                return Content($"Error creating tables: {ex.Message}");
            }
        }
    }
}
