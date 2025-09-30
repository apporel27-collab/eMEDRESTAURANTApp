#!/bin/bash

# Get the connection string from appsettings.json
CONNECTION_STRING=$(grep -o '"DefaultConnection": *"[^"]*"' ./RestaurantManagementSystem/appsettings.json | sed 's/"DefaultConnection": *"\(.*\)"/\1/')

# Extract server, database, and credentials
SERVER=$(echo $CONNECTION_STRING | grep -o 'Server=[^;]*' | sed 's/Server=//' | sed 's/tcp://')
DATABASE=$(echo $CONNECTION_STRING | grep -o 'Database=[^;]*' | sed 's/Database=//' | sed 's/Initial Catalog=//')
USER=$(echo $CONNECTION_STRING | grep -o 'User Id=[^;]*' | sed 's/User Id=//')
PASSWORD=$(echo $CONNECTION_STRING | grep -o 'Password=[^;]*' | sed 's/Password=//')

echo "Creating RestaurantSettings table directly..."

# Use sqlcmd to execute SQL with TrustServerCertificate
sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USER" -P "$PASSWORD" -i create_settings_table_direct.sql -C -t 30 -v DATABASE="$DATABASE"

# Check if sqlcmd failed (likely due to TLS issues)
if [ $? -ne 0 ]; then
    echo "Trying alternate connection with TrustServerCertificate..."
    
    # Try different approach
    cat > temp_create_table.sql << EOF
USE [$DATABASE]; 

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
    
    PRINT 'RestaurantSettings table created successfully with default settings.';
END
ELSE
BEGIN
    PRINT 'RestaurantSettings table already exists.';
END
GO
EOF

    # Try to create a temp C# program that will execute the SQL
    cat > CreateSettingsTable.cs << 'EOF'
using System;
using System.IO;
using Microsoft.Data.SqlClient;

class CreateSettingsTable
{
    static void Main()
    {
        try
        {
            string connectionString = GetConnectionString();
            Console.WriteLine("Attempting to create RestaurantSettings table using direct SQL...");
            
            string sql = File.ReadAllText("temp_create_table.sql");
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            
            Console.WriteLine("RestaurantSettings table creation completed successfully!");
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
    
    static string GetConnectionString()
    {
        string configPath = "./RestaurantManagementSystem/appsettings.json";
        string configContent = File.ReadAllText(configPath);
        
        // Simple parsing to get connection string
        int startIndex = configContent.IndexOf("\"DefaultConnection\":");
        if (startIndex == -1) throw new Exception("DefaultConnection not found in appsettings.json");
        
        startIndex = configContent.IndexOf("\"", startIndex + "\"DefaultConnection\":".Length) + 1;
        int endIndex = configContent.IndexOf("\"", startIndex);
        
        return configContent.Substring(startIndex, endIndex - startIndex);
    }
}
EOF

    echo "Compiling and running C# program to create table..."
    dotnet build RestaurantManagementSystem/RestaurantManagementSystem.csproj -c Release
    dotnet build -o temp_build CreateSettingsTable.cs /r:RestaurantManagementSystem/bin/Release/net7.0/Microsoft.Data.SqlClient.dll
    
    dotnet temp_build/CreateSettingsTable.dll
    
    # Clean up
    rm -f temp_create_table.sql
    rm -f CreateSettingsTable.cs
    rm -rf temp_build
else
    echo "SQL script executed successfully."
fi