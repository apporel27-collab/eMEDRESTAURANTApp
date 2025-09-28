using System;
using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace RestaurantManagementSystem
{
    public class SetupDatabase
    {
        public static void SetupDatabaseMain(IConfiguration configuration = null)
        {
            try
            {
                // Get connection string from appsettings.json if not provided
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();
                }

                string connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Error: Could not find DefaultConnection string in appsettings.json");
                    return;
                }

                // Get the SQL script
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "Auth_Setup.sql");
                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"Error: SQL script not found at {scriptPath}");
                    return;
                }

                string script = File.ReadAllText(scriptPath);

                // Split the script by GO statements to execute batch by batch
                string[] batches = script.Split(new[] { "GO", "Go", "go" }, StringSplitOptions.RemoveEmptyEntries);

                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to the database.");

                    foreach (string batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(batch, connection))
                            {
                                try
                                {
                                    command.ExecuteNonQuery();
                                    Console.WriteLine("Successfully executed batch.");
                                }
                                catch (SqlException ex)
                                {
                                    Console.WriteLine($"Error executing batch: {ex.Message}");
                                    Console.WriteLine($"Batch: {batch}");
                                }
                            }
                        }
                    }

                    Console.WriteLine("Database setup completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
