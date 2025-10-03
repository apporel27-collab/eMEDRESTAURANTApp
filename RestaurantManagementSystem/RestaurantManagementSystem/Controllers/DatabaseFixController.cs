using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RestaurantManagementSystem.Controllers
{
    public class DatabaseFixController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DatabaseFixController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // GET: /DatabaseFix/FixMenuItemColumns
        public IActionResult FixMenuItemColumns()
        {
            var messages = new List<string>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    messages.Add("Database connection opened successfully.");

                    // Fix PreparationTimeMinutes column
                    if (!ColumnExists(connection, "MenuItems", "PreparationTimeMinutes"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE MenuItems ADD PreparationTimeMinutes INT NOT NULL DEFAULT 15", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Added PreparationTimeMinutes column to MenuItems table.");
                        }
                    }
                    else
                    {
                        messages.Add("PreparationTimeMinutes column already exists.");
                    }
                    
                    // Fix KitchenStationId column
                    if (!ColumnExists(connection, "MenuItems", "KitchenStationId"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE MenuItems ADD KitchenStationId INT NULL", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Added KitchenStationId column to MenuItems table.");
                        }
                    }
                    else
                    {
                        messages.Add("KitchenStationId column already exists.");
                    }
                    
                    // Fix CalorieCount column
                    if (!ColumnExists(connection, "MenuItems", "CalorieCount"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE MenuItems ADD CalorieCount INT NULL", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Added CalorieCount column to MenuItems table.");
                        }
                    }
                    else
                    {
                        messages.Add("CalorieCount column already exists.");
                    }
                    
                    // Fix IsFeatured column
                    if (!ColumnExists(connection, "MenuItems", "IsFeatured"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE MenuItems ADD IsFeatured BIT NOT NULL DEFAULT 0", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Added IsFeatured column to MenuItems table.");
                        }
                    }
                    else
                    {
                        messages.Add("IsFeatured column already exists.");
                    }
                    
                    // Fix IsSpecial column
                    if (!ColumnExists(connection, "MenuItems", "IsSpecial"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE MenuItems ADD IsSpecial BIT NOT NULL DEFAULT 0", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Added IsSpecial column to MenuItems table.");
                        }
                    }
                    else
                    {
                        messages.Add("IsSpecial column already exists.");
                    }
                    
                    // Fix DiscountPercentage column
                    if (!ColumnExists(connection, "MenuItems", "DiscountPercentage"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE MenuItems ADD DiscountPercentage DECIMAL(5,2) NULL", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Added DiscountPercentage column to MenuItems table.");
                        }
                    }
                    else
                    {
                        messages.Add("DiscountPercentage column already exists.");
                    }
                    
                    messages.Add("All columns have been fixed successfully.");
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    messages.Add($"Inner Error: {ex.InnerException.Message}");
                }
            }
            
            return View("FixResult", messages);
        }
        
        private bool ColumnExists(Microsoft.Data.SqlClient.SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                @"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName",
                connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@columnName", columnName);
                
                return command.ExecuteScalar() != null;
            }
        }
        
        private bool TableExists(Microsoft.Data.SqlClient.SqlConnection connection, string tableName)
        {
            using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                @"SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @tableName",
                connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                return command.ExecuteScalar() != null;
            }
        }
        
        // GET: /DatabaseFix/CreateMenuItemTables
        public IActionResult CreateMenuItemTables()
        {
            var messages = new List<string>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    messages.Add("Database connection opened successfully.");
                    
                    // Create Ingredients table if it doesn't exist
                    if (!TableExists(connection, "Ingredients"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"CREATE TABLE [dbo].[Ingredients] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [Name] NVARCHAR(100) NOT NULL,
                                [Description] NVARCHAR(500) NULL,
                                [UnitOfMeasure] NVARCHAR(50) NULL,
                                [CurrentStock] DECIMAL(10, 2) NULL,
                                [MinimumStock] DECIMAL(10, 2) NULL,
                                [Cost] DECIMAL(10, 2) NULL,
                                [IsActive] BIT NOT NULL DEFAULT 1,
                                [CategoryId] INT NULL,
                                [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                            )",
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Created Ingredients table.");
                        }
                    }
                    else
                    {
                        messages.Add("Ingredients table already exists.");
                    }
                    
                    // Create Allergens table if it doesn't exist
                    if (!TableExists(connection, "Allergens"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"CREATE TABLE [dbo].[Allergens] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [Name] NVARCHAR(100) NOT NULL,
                                [Description] NVARCHAR(500) NULL,
                                [IsActive] BIT NOT NULL DEFAULT 1
                            )",
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Created Allergens table.");
                        }
                    }
                    else
                    {
                        messages.Add("Allergens table already exists.");
                    }
                    
                    // Create Modifiers table if it doesn't exist
                    if (!TableExists(connection, "Modifiers"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"CREATE TABLE [dbo].[Modifiers] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [Name] NVARCHAR(100) NOT NULL,
                                [Description] NVARCHAR(500) NULL,
                                [ModifierType] NVARCHAR(50) NOT NULL,
                                [IsActive] BIT NOT NULL DEFAULT 1
                            )",
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Created Modifiers table.");
                        }
                    }
                    else
                    {
                        messages.Add("Modifiers table already exists.");
                    }
                    
                    // Create MenuItemAllergens table if it doesn't exist
                    if (!TableExists(connection, "MenuItemAllergens"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"CREATE TABLE [dbo].[MenuItemAllergens] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [MenuItemId] INT NOT NULL,
                                [AllergenId] INT NOT NULL,
                                CONSTRAINT [FK_MenuItemAllergens_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems]([Id]),
                                CONSTRAINT [FK_MenuItemAllergens_Allergens] FOREIGN KEY ([AllergenId]) REFERENCES [Allergens]([Id])
                            )",
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Created MenuItemAllergens table.");
                        }
                    }
                    else
                    {
                        messages.Add("MenuItemAllergens table already exists.");
                    }
                    
                    // Create MenuItemIngredients table if it doesn't exist
                    if (!TableExists(connection, "MenuItemIngredients"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"CREATE TABLE [dbo].[MenuItemIngredients] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [MenuItemId] INT NOT NULL,
                                [IngredientId] INT NOT NULL,
                                [Quantity] DECIMAL(10, 2) NOT NULL,
                                [Unit] NVARCHAR(20) NOT NULL,
                                [IsOptional] BIT NOT NULL DEFAULT 0,
                                [Instructions] NVARCHAR(200) NULL,
                                CONSTRAINT [FK_MenuItemIngredients_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems]([Id]),
                                CONSTRAINT [FK_MenuItemIngredients_Ingredients] FOREIGN KEY ([IngredientId]) REFERENCES [Ingredients]([Id])
                            )",
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Created MenuItemIngredients table.");
                        }
                    }
                    else
                    {
                        messages.Add("MenuItemIngredients table already exists.");
                    }
                    
                    // Create MenuItemModifiers table if it doesn't exist
                    if (!TableExists(connection, "MenuItemModifiers"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"CREATE TABLE [dbo].[MenuItemModifiers] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [MenuItemId] INT NOT NULL,
                                [ModifierId] INT NOT NULL,
                                [PriceAdjustment] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                                CONSTRAINT [FK_MenuItemModifiers_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems]([Id]),
                                CONSTRAINT [FK_MenuItemModifiers_Modifiers] FOREIGN KEY ([ModifierId]) REFERENCES [Modifiers]([Id])
                            )",
                            connection))
                        {
                            command.ExecuteNonQuery();
                            messages.Add("Created MenuItemModifiers table.");
                        }
                    }
                    else
                    {
                        messages.Add("MenuItemModifiers table already exists.");
                    }
                    
                    messages.Add("All tables have been created successfully.");
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    messages.Add($"Inner Error: {ex.InnerException.Message}");
                }
            }
            
            return View("FixResult", messages);
        }
    }
}
