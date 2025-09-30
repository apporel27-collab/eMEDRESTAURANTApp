#!/bin/bash

# Set up variables
SQL_FILE="/Users/abhikporel/dev/Restaurantapp/check_settings_schema.sql"
CONNECTION_STRING="Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=5;"

# Run SQL command using dotnet
cd /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem

# Create C# program to execute SQL
cat > ExecuteSQL.cs << EOF
using System;
using System.IO;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "$CONNECTION_STRING";
        string sql = File.ReadAllText("$SQL_FILE");

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected to SQL Server");
                
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("SQL script executed successfully!");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(\$"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine(\$"Inner Exception: {ex.InnerException.Message}");
            }
            Environment.Exit(1);
        }
    }
}
EOF

# Compile and run the C# program
dotnet new console -n SqlExecutor -o SqlExecutor
cp ExecuteSQL.cs SqlExecutor/Program.cs
cd SqlExecutor
dotnet add package Microsoft.Data.SqlClient
dotnet run

# Clean up
cd ..
rm -rf SqlExecutor
rm ExecuteSQL.cs

echo "SQL execution completed."