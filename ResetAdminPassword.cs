using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using BCrypt.Net;

namespace RestaurantManagementSystem
{
    class ResetAdminPassword
    {
        static void Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .Build();

            // Get connection string
            string connectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine("Connecting to database to reset admin password...");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Successfully connected to database.");

                    // Generate a BCrypt hash for "Admin@123"
                    string newPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    Console.WriteLine($"Generated new password hash: {newPasswordHash}");

                    // Update the admin user's password
                    using (var command = new SqlCommand("UPDATE Users SET PasswordHash = @PasswordHash WHERE Username = 'admin'", connection))
                    {
                        command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("Admin password has been reset successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Admin user not found.");
                        }
                    }

                    // Verify the admin user's password
                    using (var command = new SqlCommand("SELECT PasswordHash FROM Users WHERE Username = 'admin'", connection))
                    {
                        string passwordHash = (string)command.ExecuteScalar();
                        Console.WriteLine($"Updated password hash: {passwordHash}");
                        
                        // Test if the password matches "Admin@123"
                        bool passwordMatches = BCrypt.Net.BCrypt.Verify("Admin@123", passwordHash);
                        Console.WriteLine($"Password 'Admin@123' matches: {passwordMatches}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
