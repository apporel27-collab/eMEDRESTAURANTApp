using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RestaurantManagementSystem
{
    class AdminCheck
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
            Console.WriteLine("Checking admin user in database...");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Successfully connected to database.");

                    // Check if admin user exists
                    using (var command = new SqlCommand("SELECT Id, Username, PasswordHash, FirstName, LastName, IsActive, IsLockedOut FROM Users WHERE Username = 'admin'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string username = reader.GetString(1);
                                string passwordHash = reader.GetString(2);
                                string firstName = reader.GetString(3);
                                string lastName = reader.IsDBNull(4) ? null : reader.GetString(4);
                                bool isActive = reader.GetBoolean(5);
                                bool isLockedOut = reader.GetBoolean(6);

                                Console.WriteLine($"Admin user found with ID: {id}");
                                Console.WriteLine($"Username: {username}");
                                Console.WriteLine($"Password Hash: {passwordHash}");
                                Console.WriteLine($"Name: {firstName} {lastName}");
                                Console.WriteLine($"Active: {isActive}");
                                Console.WriteLine($"Locked Out: {isLockedOut}");

                                // Test if the password matches "Admin@123"
                                bool passwordMatches = BCrypt.Net.BCrypt.Verify("Admin@123", passwordHash);
                                Console.WriteLine($"Password 'Admin@123' matches: {passwordMatches}");
                            }
                            else
                            {
                                Console.WriteLine("Admin user not found in database.");
                            }
                        }
                    }

                    // Check roles assigned to admin user
                    using (var command = new SqlCommand(
                        @"SELECT r.Id, r.Name
                          FROM Roles r
                          JOIN UserRoles ur ON r.Id = ur.RoleId
                          JOIN Users u ON u.Id = ur.UserId
                          WHERE u.Username = 'admin'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            Console.WriteLine("\nRoles assigned to admin:");
                            bool hasRoles = false;
                            while (reader.Read())
                            {
                                hasRoles = true;
                                int roleId = reader.GetInt32(0);
                                string roleName = reader.GetString(1);
                                Console.WriteLine($"- Role ID: {roleId}, Name: {roleName}");
                            }

                            if (!hasRoles)
                            {
                                Console.WriteLine("No roles assigned to admin user.");
                            }
                        }
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
