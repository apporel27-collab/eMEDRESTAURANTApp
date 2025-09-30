using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main()
    {
        try
        {
            // Read appsettings.json to get connection string
            string appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "RestaurantManagementSystem", "appsettings.json");
            string json = File.ReadAllText(appSettingsPath);
            
            // Parse JSON
            using JsonDocument doc = JsonDocument.Parse(json);
            string connectionString = doc.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("DefaultConnection")
                .GetString();
            
            // Add TrustServerCertificate=True if it's not already there
            if (!connectionString.Contains("TrustServerCertificate=True"))
            {
                connectionString += ";TrustServerCertificate=True";
            }
            
            Console.WriteLine("Using connection string (with sensitive info masked):");
            Console.WriteLine(MaskConnectionString(connectionString));
            
            // SQL script to create RestaurantSettings table
            string createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RestaurantSettings')
BEGIN
    CREATE TABLE RestaurantSettings (
        Id INT PRIMARY KEY IDENTITY(1,1),
        RestaurantName NVARCHAR(100) NOT NULL,
        Address NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(20) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        Logo NVARCHAR(MAX) NULL,
        TaxRate DECIMAL(5,2) DEFAULT 0.00,
        CurrencySymbol NVARCHAR(5) DEFAULT '$',
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME DEFAULT GETDATE()
    );
    
    -- Insert default settings
    INSERT INTO RestaurantSettings (RestaurantName, Address, Phone, Email, TaxRate, CurrencySymbol)
    VALUES ('My Restaurant', '123 Main St', '555-123-4567', 'contact@myrestaurant.com', 10.00, '$');
    
    PRINT 'RestaurantSettings table created and default data inserted.';
END
ELSE
BEGIN
    PRINT 'RestaurantSettings table already exists.';
END";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected to database successfully!");
                
                using (SqlCommand command = new SqlCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("SQL script executed successfully!");
                }
            }
            
            Console.WriteLine("Operation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }
    
    static string MaskConnectionString(string connectionString)
    {
        // Simple masking of password and other sensitive parts
        return connectionString
            .Replace(GetValueFromConnectionString(connectionString, "Password"), "********")
            .Replace(GetValueFromConnectionString(connectionString, "User ID"), "********")
            .Replace(GetValueFromConnectionString(connectionString, "Initial Catalog"), "********");
    }
    
    static string GetValueFromConnectionString(string connectionString, string key)
    {
        string keyPattern = key + "=";
        int startIndex = connectionString.IndexOf(keyPattern);
        
        if (startIndex < 0)
            return string.Empty;
            
        startIndex += keyPattern.Length;
        int endIndex = connectionString.IndexOf(';', startIndex);
        
        if (endIndex < 0)
            endIndex = connectionString.Length;
            
        return connectionString.Substring(startIndex, endIndex - startIndex);
    }
}