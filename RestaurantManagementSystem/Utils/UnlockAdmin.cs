using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace RestaurantManagementSystem.Utils
{
    public class UnlockAdminUtil
    {
        public static void UnlockAdmin(IConfiguration configuration = null)
        {
            try
            {
                // Build configuration if not provided
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();
                }

                // Get connection string from configuration
                string connectionString = configuration.GetConnectionString("DefaultConnection");

                // SQL to unlock admin account
                string sql = @"
                    -- Unlock the admin user account
                    UPDATE Users 
                    SET IsLockedOut = 0, FailedLoginAttempts = 0
                    WHERE Username = 'admin';

                    -- Confirm the update
                    SELECT Username, IsLockedOut, FailedLoginAttempts 
                    FROM Users
                    WHERE Username = 'admin';";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to database.");

                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            // Process results from the SELECT statement
                            if (reader.Read())
                            {
                                string username = reader["Username"].ToString();
                                bool isLocked = (bool)reader["IsLockedOut"];
                                int failedAttempts = (int)reader["FailedLoginAttempts"];

                                Console.WriteLine($"User: {username}");
                                Console.WriteLine($"Locked: {isLocked}");
                                Console.WriteLine($"Failed Attempts: {failedAttempts}");
                                
                                if (!isLocked)
                                {
                                    Console.WriteLine("Admin account has been successfully unlocked.");
                                }
                                else
                                {
                                    Console.WriteLine("Failed to unlock admin account.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Admin user not found.");
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

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
