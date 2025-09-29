#!/bin/bash

# Define variables
SQL_FILE="/Users/abhikporel/dev/Restaurantapp/fix_payment_sp.sql"
CONNECTION_STRING="Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=5;"
SERVER="192.250.231.28,1433"
DATABASE="dev_Restaurant"
USER="purojit2_idmcbp"
PASSWORD="45*8qce8E"

echo "Installing payment stored procedure..."

# Using sqlcmd with the correct parameters and trust server certificate
sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USER" -P "$PASSWORD" -i "$SQL_FILE" -N -C -t 60

# Check if sqlcmd was successful
if [ $? -eq 0 ]; then
    echo "Payment stored procedure installed successfully."
else
    echo "Error installing payment stored procedure. Please check the SQL script and connection details."
    
    # Alternative approach using dotnet
    echo "Trying alternative approach with .NET..."
    
    # Create a temporary C# program to execute the SQL
    cat > ExecuteSql.cs << 'EOL'
using System;
using System.IO;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = args[0];
        string sqlFilePath = args[1];
        
        try
        {
            string sqlContent = File.ReadAllText(sqlFilePath);
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected to database successfully.");
                
                using (SqlCommand command = new SqlCommand(sqlContent, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("SQL executed successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
EOL
    
    # Compile and run the C# program
    dotnet new console -n SqlExecutor -o SqlExecutor
    cp ExecuteSql.cs SqlExecutor/Program.cs
    cd SqlExecutor
    dotnet add package Microsoft.Data.SqlClient
    dotnet build
    dotnet run "$CONNECTION_STRING" "../$SQL_FILE"
    cd ..
    rm -rf SqlExecutor
    rm ExecuteSql.cs
fi