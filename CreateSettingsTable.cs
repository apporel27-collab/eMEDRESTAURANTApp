using System;
using System.IO;
using Microsoft.Data.SqlClient;

class CreateSettingsTable
{
    static void Main()
    {
        try
        {
            // Read connection string from appsettings.json
            string configPath = "./RestaurantManagementSystem/appsettings.json";
            string configContent = File.ReadAllText(configPath);
            
            // Extract connection string using simple parsing
            int startIndex = configContent.IndexOf("\"DefaultConnection\":");
            if (startIndex == -1) throw new Exception("DefaultConnection not found in appsettings.json");
            
            startIndex = configContent.IndexOf("\"", startIndex + "\"DefaultConnection\":".Length) + 1;
            int endIndex = configContent.IndexOf("\"", startIndex);
            string connectionString = configContent.Substring(startIndex, endIndex - startIndex);
            
            // Ensure TrustServerCertificate=True is in the connection string
            if (!connectionString.Contains("TrustServerCertificate=True"))
            {
                connectionString += ";TrustServerCertificate=True";
            }
            
            Console.WriteLine("Attempting to create RestaurantSettings table...");
            
            string sql = @"
-- Check if the RestaurantSettings table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RestaurantSettings')
BEGIN
    -- Create RestaurantSettings table
    CREATE TABLE [dbo].[RestaurantSettings] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [RestaurantName] NVARCHAR(100) NOT NULL,
        [StreetAddress] NVARCHAR(200) NOT NULL,
        [City] NVARCHAR(50) NOT NULL,
        [State] NVARCHAR(50) NOT NULL,
        [Pincode] NVARCHAR(10) NOT NULL,
        [Country] NVARCHAR(50) NOT NULL,
        [GSTCode] NVARCHAR(15) NOT NULL,
        [PhoneNumber] NVARCHAR(15) NULL,
        [Email] NVARCHAR(100) NULL,
        [Website] NVARCHAR(100) NULL,
        [LogoPath] NVARCHAR(200) NULL,
        [CurrencySymbol] NVARCHAR(50) NOT NULL DEFAULT N'₹',
        [DefaultGSTPercentage] DECIMAL(5,2) NOT NULL DEFAULT 5.00,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    -- Insert default restaurant settings
    INSERT INTO [dbo].[RestaurantSettings] (
        [RestaurantName], 
        [StreetAddress], 
        [City], 
        [State], 
        [Pincode], 
        [Country], 
        [GSTCode],
        [PhoneNumber],
        [Email],
        [Website],
        [CurrencySymbol],
        [DefaultGSTPercentage]
    )
    VALUES (
        'My Restaurant',
        'Sample Street Address',
        'Mumbai',
        'Maharashtra',
        '400001',
        'India',
        '27AAPFU0939F1ZV',
        '+919876543210',
        'info@myrestaurant.com',
        'https://www.myrestaurant.com',
        '₹',
        5.00
    );
    
    SELECT 'RestaurantSettings table created successfully with default settings.' AS Result;
END
ELSE
BEGIN
    SELECT 'RestaurantSettings table already exists.' AS Result;
END";
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected to database successfully");
                
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Result"].ToString());
                        }
                    }
                }
            }
            
            Console.WriteLine("Operation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
    }
}