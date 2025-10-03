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

                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();
                    

                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            // Process results from the SELECT statement
                            if (reader.Read())
                            {
                                string username = reader["Username"].ToString();
                                bool isLocked = (bool)reader["IsLockedOut"];
                                int failedAttempts = (int)reader["FailedLoginAttempts"];

                                
                                
                                
                                
                                if (!isLocked)
                                {
                                    
                                }
                                else
                                {
                                    
                                }
                            }
                            else
                            {
                                
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
                if (ex.InnerException != null)
                {
                    
                }
            }

            
            Console.ReadKey();
        }
    }
}
