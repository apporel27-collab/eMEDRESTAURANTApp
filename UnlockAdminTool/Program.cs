using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace UnlockAdminTool
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Build configuration
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.Development.json", optional: true);
                
                var config = configBuilder.Build();

                // Get connection string
                var connectionString = config.GetConnectionString("DefaultConnection");
                Console.WriteLine($"Attempting to connect using: {connectionString}");
                
                // SQL to unlock or create admin account
                string checkUserExistsSql = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
                string createUserSql = @"
                    INSERT INTO Users (Username, Password, Email, FullName, RoleId, IsLockedOut, FailedLoginAttempts)
                    VALUES ('admin', 'password', 'admin@restaurant.com', 'System Administrator', 1, 0, 0)";
                string unlockUserSql = @"
                    UPDATE Users 
                    SET IsLockedOut = 0, FailedLoginAttempts = 0, Password = 'password'
                    WHERE Username = 'admin'";
                
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to database successfully.");
                    
                    // Check if admin user exists
                    int userCount = 0;
                    using (var command = new SqlCommand(checkUserExistsSql, connection))
                    {
                        try {
                            userCount = (int)command.ExecuteScalar();
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Error checking for admin user: {ex.Message}");
                            Console.WriteLine("This might happen if the Users table doesn't exist or has a different schema.");
                            return;
                        }
                    }
                    
                    if (userCount > 0)
                    {
                        // User exists, unlock it
                        using (var command = new SqlCommand(unlockUserSql, connection))
                        {
                            int result = command.ExecuteNonQuery();
                            if (result > 0)
                            {
                                Console.WriteLine("Admin account has been successfully unlocked!");
                                Console.WriteLine("Username: admin");
                                Console.WriteLine("Password: password");
                            }
                            else
                            {
                                Console.WriteLine("Failed to update admin account.");
                            }
                        }
                    }
                    else
                    {
                        // User doesn't exist, create it
                        using (var command = new SqlCommand(createUserSql, connection))
                        {
                            try {
                                int result = command.ExecuteNonQuery();
                                if (result > 0)
                                {
                                    Console.WriteLine("Admin account has been successfully created!");
                                    Console.WriteLine("Username: admin");
                                    Console.WriteLine("Password: password");
                                }
                                else
                                {
                                    Console.WriteLine("Failed to create admin account.");
                                }
                            }
                            catch (Exception ex) {
                                Console.WriteLine($"Error creating admin user: {ex.Message}");
                                Console.WriteLine("This might happen if the Users table doesn't have the expected schema.");
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
