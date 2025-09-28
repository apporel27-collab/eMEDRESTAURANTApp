using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace RestaurantManagementSystem.Middleware
{
    public class DatabaseColumnFixMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DatabaseColumnFixMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to apply database fixes before continuing
            TryFixDatabaseColumns();
            
            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }

        private void TryFixDatabaseColumns()
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Fix Tables table
                    if (!ColumnExists(connection, "Tables", "TableName"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"ALTER TABLE [dbo].[Tables] 
                            ADD [TableName] AS ([TableNumber])", connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added TableName computed column to Tables table.");
                        }
                    }

                    // Fix Users table
                    if (!ColumnExists(connection, "Users", "FullName"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"ALTER TABLE [dbo].[Users] 
                            ADD [FullName] AS (LTRIM(RTRIM(ISNULL([FirstName], '') + ' ' + ISNULL([LastName], ''))))", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added FullName computed column to Users table.");
                        }
                    }
                    
                    // Add Phone column if it doesn't exist
                    if (!ColumnExists(connection, "Users", "Phone"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(20) NULL", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added Phone column to Users table.");
                        }
                    }
                    
                    // Add Role column if it doesn't exist - maps RoleId to Role
                    if (!ColumnExists(connection, "Users", "Role"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE [dbo].[Users] ADD [Role] INT NOT NULL DEFAULT 3", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added Role column to Users table.");
                        }
                    }

                    // Only update Role from RoleId if BOTH columns exist
                    if (ColumnExists(connection, "Users", "Role") && ColumnExists(connection, "Users", "RoleId"))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "UPDATE [dbo].[Users] SET [Role] = [RoleId] WHERE [Role] <> [RoleId]", 
                            connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Updated Role column with RoleId values.");
                        }
                    }
                    
                    // Check for missing tables and create them if needed
                    CreateRequiredTablesIfMissing(connection);
                    
                    // Fix MenuItem columns
                    FixMenuItemColumns(connection);
                    
                    // Check for correct columns in existing tables
                    FixExistingTableColumns(connection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing database columns: {ex.Message}");
            }
        }

        private bool ColumnExists(Microsoft.Data.SqlClient.SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                $"SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.{tableName}') AND name = '{columnName}'", 
                connection))
            {
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }
        
        private bool TableExists(Microsoft.Data.SqlClient.SqlConnection connection, string tableName)
        {
            using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                $"SELECT COUNT(1) FROM sys.tables WHERE name = '{tableName}'", 
                connection))
            {
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }
        
        private void CreateRequiredTablesIfMissing(Microsoft.Data.SqlClient.SqlConnection connection)
        {
            try
            {
                // Create Categories table (base table)
                if (!TableExists(connection, "Categories"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[Categories] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [CategoryName] NVARCHAR(100) NOT NULL,
                            [IsActive] BIT NOT NULL DEFAULT 1
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created Categories table.");
                    }
                }
                
                // Create Ingredients table
                if (!TableExists(connection, "Ingredients"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[Ingredients] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [IngredientsName] NVARCHAR(100) NOT NULL,
                            [DisplayName] NVARCHAR(100) NULL,
                            [Code] NVARCHAR(20) NULL
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created Ingredients table.");
                    }
                }
                
                // Create CourseTypes table
                if (!TableExists(connection, "CourseTypes"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[CourseTypes] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Name] NVARCHAR(50) NOT NULL,
                            [DisplayOrder] INT NOT NULL DEFAULT 0,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created CourseTypes table.");
                        
                        // Insert default course types
                        using (var insertCommand = new Microsoft.Data.SqlClient.SqlCommand(
                            @"INSERT INTO [CourseTypes] ([Name], [DisplayOrder])
                            VALUES 
                                ('Appetizer', 1),
                                ('Soup/Salad', 2),
                                ('Main Course', 3),
                                ('Dessert', 4),
                                ('Beverage', 5)", connection))
                        {
                            insertCommand.ExecuteNonQuery();
                            Console.WriteLine("Added default course types.");
                        }
                    }
                }
                
                // Create MenuItems table (depends on Categories)
                if (!TableExists(connection, "MenuItems") && TableExists(connection, "Categories"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[MenuItems] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Name] NVARCHAR(100) NOT NULL,
                            [Description] NVARCHAR(500) NULL,
                            [Price] DECIMAL(10, 2) NOT NULL,
                            [CategoryId] INT NOT NULL,
                            [IsAvailable] BIT NOT NULL DEFAULT 1,
                            [PrepTime] INT NULL,
                            [ImagePath] NVARCHAR(255) NULL,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT [FK_MenuItems_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Categories]([Id])
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created MenuItems table.");
                    }
                }

                // Create Tables table
                if (!TableExists(connection, "Tables"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[Tables] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [TableNumber] NVARCHAR(10) NOT NULL,
                            [Capacity] INT NOT NULL,
                            [Status] INT NOT NULL DEFAULT 0,
                            [IsActive] BIT NOT NULL DEFAULT 1
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created Tables table.");
                    }
                }
                
                // Create Reservations table
                if (!TableExists(connection, "Reservations"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[Reservations] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [GuestName] NVARCHAR(100) NOT NULL,
                            [PhoneNumber] NVARCHAR(20) NOT NULL,
                            [EmailAddress] NVARCHAR(100) NULL,
                            [PartySize] INT NOT NULL,
                            [ReservationDate] DATE NOT NULL,
                            [ReservationTime] DATETIME NOT NULL,
                            [SpecialRequests] NVARCHAR(200) NULL,
                            [Notes] NVARCHAR(500) NULL,
                            [TableId] INT NULL,
                            [Status] INT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [ReminderSent] BIT NOT NULL DEFAULT 0,
                            [NoShow] BIT NOT NULL DEFAULT 0,
                            CONSTRAINT [FK_Reservations_Tables] FOREIGN KEY ([TableId]) REFERENCES [Tables]([Id])
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created Reservations table.");
                    }
                }
                
                // Create Users table
                if (!TableExists(connection, "Users"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[Users] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Username] NVARCHAR(50) NOT NULL,
                            [Password] NVARCHAR(100) NOT NULL,
                            [FirstName] NVARCHAR(50) NULL,
                            [LastName] NVARCHAR(50) NULL,
                            [Email] NVARCHAR(100) NULL,
                            [Phone] NVARCHAR(20) NULL,
                            [Role] INT NOT NULL DEFAULT 0,
                            [IsActive] BIT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [FullName] AS (LTRIM(RTRIM(ISNULL([FirstName], '') + ' ' + ISNULL([LastName], ''))))
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created Users table with FullName computed column.");
                    }
                }
                
                // Create TableTurnovers table (depends on Tables)
                if (!TableExists(connection, "TableTurnovers") && TableExists(connection, "Tables"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[TableTurnovers] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [TableId] INT NOT NULL,
                            [PartySize] INT NOT NULL,
                            [StartTime] DATETIME NOT NULL DEFAULT GETDATE(),
                            [EndTime] DATETIME NULL,
                            [Status] INT NOT NULL DEFAULT 0,
                            [GuestName] NVARCHAR(100) NULL,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT [FK_TableTurnovers_Tables] FOREIGN KEY ([TableId]) REFERENCES [Tables]([Id])
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created TableTurnovers table.");
                    }
                }
                
                // Create Orders table (depends on TableTurnovers and Users)
                if (!TableExists(connection, "Orders") && TableExists(connection, "TableTurnovers") && TableExists(connection, "Users"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[Orders] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [OrderNumber] NVARCHAR(20) NOT NULL,
                            [TableTurnoverId] INT NULL,
                            [OrderType] INT NOT NULL,
                            [Status] INT NOT NULL DEFAULT 0,
                            [UserId] INT NULL,
                            [CustomerName] NVARCHAR(100) NULL,
                            [CustomerPhone] NVARCHAR(20) NULL,
                            [Subtotal] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                            [TaxAmount] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                            [TipAmount] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                            [DiscountAmount] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                            [TotalAmount] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                            [SpecialInstructions] NVARCHAR(500) NULL,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [CompletedAt] DATETIME NULL,
                            CONSTRAINT [FK_Orders_TableTurnovers] FOREIGN KEY ([TableTurnoverId]) REFERENCES [TableTurnovers]([Id]),
                            CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created Orders table.");
                    }
                }
                
                // Create OrderItems table (depends on Orders, MenuItems, and CourseTypes)
                if (!TableExists(connection, "OrderItems") && TableExists(connection, "Orders") && 
                    TableExists(connection, "MenuItems") && TableExists(connection, "CourseTypes"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"CREATE TABLE [dbo].[OrderItems] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [OrderId] INT NOT NULL,
                            [MenuItemId] INT NOT NULL,
                            [Quantity] INT NOT NULL DEFAULT 1,
                            [UnitPrice] DECIMAL(10, 2) NOT NULL,
                            [Subtotal] DECIMAL(10, 2) NOT NULL,
                            [SpecialInstructions] NVARCHAR(500) NULL,
                            [CourseId] INT NULL,
                            [Status] INT NOT NULL DEFAULT 0,
                            [FireTime] DATETIME NULL,
                            [CompletionTime] DATETIME NULL,
                            [DeliveryTime] DATETIME NULL,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [Orders]([Id]),
                            CONSTRAINT [FK_OrderItems_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems]([Id]),
                            CONSTRAINT [FK_OrderItems_CourseTypes] FOREIGN KEY ([CourseId]) REFERENCES [CourseTypes]([Id])
                        )", connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Created OrderItems table.");
                    }
                }
                
                // Additional tables can be created as needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating tables: {ex.Message}");
            }
        }
        
        private void FixExistingTableColumns(Microsoft.Data.SqlClient.SqlConnection connection)
        {
            try
            {
                // Fix Categories table columns
                if (TableExists(connection, "Categories"))
                {
                    Console.WriteLine("Categories table exists, checking columns...");
                    
                    // Always check for and drop the CategoryName computed column if it exists
                    // since we're only using Name now
                    if (ColumnExists(connection, "Categories", "CategoryName"))
                    {
                        try 
                        {
                            using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                                "ALTER TABLE [dbo].[Categories] DROP COLUMN [CategoryName]", connection))
                            {
                                command.ExecuteNonQuery();
                                Console.WriteLine("Removed CategoryName computed column from Categories table.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Couldn't drop CategoryName column: {ex.Message}");
                        }
                    }
                    
                    // Make sure the Name column exists - it's the main column for storage
                    if (!ColumnExists(connection, "Categories", "Name"))
                    {
                        Console.WriteLine("Categories table is missing the Name column, adding it...");
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE [dbo].[Categories] ADD [Name] NVARCHAR(100) NOT NULL DEFAULT ''", connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added Name column to Categories table.");
                        }
                    }
                    
                    // Add IsActive column if it doesn't exist
                    if (!ColumnExists(connection, "Categories", "IsActive"))
                    {
                        Console.WriteLine("Adding IsActive column to Categories table...");
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE [dbo].[Categories] ADD [IsActive] BIT NOT NULL DEFAULT 1", connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added IsActive column to Categories table.");
                        }
                    }
                }
                
                // Fix Ingredients table columns
                if (TableExists(connection, "Ingredients"))
                {
                    Console.WriteLine("Ingredients table exists, checking columns...");
                    
                    // Check if Name column exists but IngredientsName doesn't
                    if (ColumnExists(connection, "Ingredients", "Name") && !ColumnExists(connection, "Ingredients", "IngredientsName"))
                    {
                        Console.WriteLine("Converting Name to IngredientsName in Ingredients table...");
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            @"
                            BEGIN TRANSACTION;
                            
                            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ingredients') AND name = 'Name')
                            BEGIN
                                -- Step 1: Add IngredientsName column
                                ALTER TABLE [dbo].[Ingredients] ADD [IngredientsName] NVARCHAR(100);
                                
                                -- Step 2: Copy data from Name to IngredientsName
                                UPDATE [dbo].[Ingredients] SET [IngredientsName] = [Name];
                            END
                            
                            COMMIT;", connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added and populated IngredientsName column in Ingredients table.");
                        }
                    }
                    
                    // Add DisplayName column if it doesn't exist
                    if (!ColumnExists(connection, "Ingredients", "DisplayName"))
                    {
                        Console.WriteLine("Adding DisplayName column to Ingredients table...");
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE [dbo].[Ingredients] ADD [DisplayName] NVARCHAR(100) NULL", connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added DisplayName column to Ingredients table.");
                        }
                    }
                    
                    // Add Code column if it doesn't exist
                    if (!ColumnExists(connection, "Ingredients", "Code"))
                    {
                        Console.WriteLine("Adding Code column to Ingredients table...");
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER TABLE [dbo].[Ingredients] ADD [Code] NVARCHAR(20) NULL", connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("Added Code column to Ingredients table.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing table columns: {ex.Message}");
            }
        }

        private void FixMenuItemColumns(Microsoft.Data.SqlClient.SqlConnection connection)
        {
            try
            {
                // Check if MenuItems table exists
                if (!TableExists(connection, "MenuItems"))
                {
                    Console.WriteLine("MenuItems table doesn't exist yet, skipping column fixes.");
                    return;
                }
                
                // Check and add PreparationTimeMinutes column
                if (!ColumnExists(connection, "MenuItems", "PreparationTimeMinutes"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD PreparationTimeMinutes INT NOT NULL DEFAULT 15", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added PreparationTimeMinutes column to MenuItems table.");
                    }
                }
                
                // Check and add KitchenStationId column
                if (!ColumnExists(connection, "MenuItems", "KitchenStationId"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD KitchenStationId INT NULL", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added KitchenStationId column to MenuItems table.");
                    }
                }
                
                // Check and add CalorieCount column
                if (!ColumnExists(connection, "MenuItems", "CalorieCount"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD CalorieCount INT NULL", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added CalorieCount column to MenuItems table.");
                    }
                }
                
                // Check and add IsFeatured column
                if (!ColumnExists(connection, "MenuItems", "IsFeatured"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD IsFeatured BIT NOT NULL DEFAULT 0", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added IsFeatured column to MenuItems table.");
                    }
                }
                
                // Check and add IsSpecial column
                if (!ColumnExists(connection, "MenuItems", "IsSpecial"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD IsSpecial BIT NOT NULL DEFAULT 0", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added IsSpecial column to MenuItems table.");
                    }
                }
                
                // Check and add DiscountPercentage column
                if (!ColumnExists(connection, "MenuItems", "DiscountPercentage"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD DiscountPercentage DECIMAL(5,2) NULL", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added DiscountPercentage column to MenuItems table.");
                    }
                }
                
                // Check and add PLUCode column if it doesn't exist
                if (!ColumnExists(connection, "MenuItems", "PLUCode"))
                {
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "ALTER TABLE MenuItems ADD PLUCode NVARCHAR(20) NULL", 
                        connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Added PLUCode column to MenuItems table.");
                    }
                }
                
                Console.WriteLine("MenuItems table columns check complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing MenuItems table columns: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class DatabaseColumnFixMiddlewareExtensions
    {
        public static IApplicationBuilder UseDatabaseColumnFixes(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DatabaseColumnFixMiddleware>();
        }
    }
}
