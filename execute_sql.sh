#!/bin/bash

# Set up variables
SQL_FILE="/Users/abhikporel/dev/Restaurantapp/fix_payment_sp.sql"
CONNECTION_STRING="Server=localhost;Database=RestaurantDB;User Id=sa;Password=YourStrong!Passw0rd;"

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
                    Console.WriteLine("SQL script executed successfully");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
EOF

# Compile and run
dotnet new console -n SQLRunner -o SQLRunner
cp ExecuteSQL.cs SQLRunner/Program.cs
cd SQLRunner
dotnet add package Microsoft.Data.SqlClient
dotnet run

# Clean up
cd ..
rm -rf SQLRunner
rm ExecuteSQL.cs