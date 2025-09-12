using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CheckDbSchema
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Build configuration to get connection string
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = configuration.GetConnectionString("DefaultConnection");
                
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to database.");

                    // Check the Users table schema
                    string checkSchemaQuery = @"
                        SELECT COLUMN_NAME, DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Users'
                        ORDER BY ORDINAL_POSITION;
                    ";

                    using (SqlCommand command = new SqlCommand(checkSchemaQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            Console.WriteLine("Users table schema:");
                            while (reader.Read())
                            {
                                string columnName = reader["COLUMN_NAME"].ToString();
                                string dataType = reader["DATA_TYPE"].ToString();
                                Console.WriteLine($"Column: {columnName}, Type: {dataType}");
                            }
                        }
                    }
                    
                    // Check if admin user exists
                    string checkAdminQuery = @"
                        SELECT * FROM Users WHERE Username = 'admin'
                    ";
                    
                    using (SqlCommand command = new SqlCommand(checkAdminQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.WriteLine("\nAdmin user exists:");
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    string value = reader[i].ToString();
                                    Console.WriteLine($"{columnName}: {value}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("\nAdmin user does not exist");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
