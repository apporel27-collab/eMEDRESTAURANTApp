using RestaurantManagementSystem.Utilities;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;

namespace RestaurantManagementSystem
{
    public class UnlockAdmin
    {
        public static void UnlockAdminAccount(IConfiguration configuration = null)
        {
            // Build configuration if not provided
            if (configuration == null)
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                    .Build();
            }

            try
            {
                // Path to the unlock script
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "Unlock_Admin_Account.sql");
                
                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"Error: Unlock script not found at {scriptPath}");
                    return;
                }
                
                Console.WriteLine("Executing script to unlock admin account...");
                // Using direct SQL execution since ExecuteSqlScript may not be available
                string connectionString = configuration.GetConnectionString("DefaultConnection");
                ExecuteSql(connectionString, scriptPath);
                Console.WriteLine("Admin account has been unlocked.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        // Local implementation of SQL execution to avoid dependency issues
        private static void ExecuteSql(string connectionString, string scriptPath)
        {
            string script = File.ReadAllText(scriptPath);
            string[] batches = script.Split(new[] { "GO", "Go", "go" }, StringSplitOptions.RemoveEmptyEntries);
            
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected to the database.");
                
                foreach (string batch in batches)
                {
                    if (!string.IsNullOrWhiteSpace(batch))
                    {
                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(batch, connection))
                        {
                            try
                            {
                                command.ExecuteNonQuery();
                                Console.WriteLine("Successfully executed batch.");
                            }
                            catch (Microsoft.Data.SqlClient.SqlException ex)
                            {
                                Console.WriteLine($"Error executing batch: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
    }
}
