using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RestaurantManagementSystem.Utilities
{
    public static class SetupDatabase
    {
        public static void ExecuteSqlScript(IConfiguration configuration, string scriptPath)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                // Read the SQL script content
                string script = File.ReadAllText(scriptPath);
                
                // Split the script by GO statements to execute each batch separately
                string[] batches = script.Split(new string[] { "GO", "go" }, StringSplitOptions.RemoveEmptyEntries);
                
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine($"Connected to database. Executing script: {scriptPath}");
                    
                    // Execute each batch
                    foreach (string batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(batch, connection))
                            {
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (SqlException ex)
                                {
                                    Console.WriteLine($"Error executing batch: {ex.Message}");
                                    Console.WriteLine($"Batch content: {batch.Substring(0, Math.Min(100, batch.Length))}...");
                                    // Continue with next batch instead of throwing
                                }
                            }
                        }
                    }
                    
                    Console.WriteLine("Script execution completed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL script: {ex.Message}");
                throw;
            }
        }
    }
}
