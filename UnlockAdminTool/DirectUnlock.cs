using System;
using System.Data.SqlClient; // Using the appropriate SqlClient namespace
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DirectUnlockTool
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

                    // Execute direct SQL to create or update admin user with fixed password
                    string sql = @"
                        -- Check if the admin user exists
                        IF EXISTS (SELECT * FROM dbo.Users WHERE Username = 'admin')
                        BEGIN
                            -- Update the existing admin user
                            UPDATE dbo.Users
                            SET Password = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918',
                                Salt = 'abc123',
                                IsLockedOut = 0,
                                FailedLoginAttempts = 0
                            WHERE Username = 'admin';
                            PRINT 'Admin user updated successfully';
                        END
                        ELSE
                        BEGIN
                            -- Create a new admin user if it doesn't exist
                            INSERT INTO dbo.Users (Username, Password, Email, FullName, RoleId, IsLockedOut, FailedLoginAttempts, Salt)
                            VALUES ('admin', '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 
                                   'admin@restaurant.com', 'System Administrator', 1, 0, 0, 'abc123');
                            PRINT 'Admin user created successfully';
                        END
                    ";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Admin user setup completed successfully.");
                    }

                    // Check if user exists and show details
                    string checkSql = "SELECT * FROM dbo.Users WHERE Username = 'admin'";
                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.WriteLine("Admin user details:");
                                Console.WriteLine($"Username: {reader["Username"]}");
                                Console.WriteLine($"IsLockedOut: {reader["IsLockedOut"]}");
                                Console.WriteLine($"FailedLoginAttempts: {reader["FailedLoginAttempts"]}");
                                Console.WriteLine("Admin login has been successfully set up.");
                                Console.WriteLine("You can now log in with username 'admin' and password 'password'");
                            }
                            else
                            {
                                Console.WriteLine("Admin user was not found after setup operation!");
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
