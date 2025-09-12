using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RestaurantManagementSystem
{
    public class CheckUserTable
    {
        public static void CheckDatabase(IConfiguration configuration)
        {
            if (configuration == null)
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
            }

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            Console.WriteLine("Checking Users table structure...");
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connection successful. Database connected.");

                    // Check if Users table exists
                    string checkTableQuery = @"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
                            SELECT 'Table exists' as Status
                        ELSE
                            SELECT 'Table does not exist' as Status";

                    using (SqlCommand cmd = new SqlCommand(checkTableQuery, connection))
                    {
                        string tableStatus = cmd.ExecuteScalar().ToString();
                        Console.WriteLine($"Users table status: {tableStatus}");
                    }

                    // If table exists, check its structure
                    string getColumnsQuery = @"
                        SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'Users'
                        ORDER BY ORDINAL_POSITION";

                    using (SqlCommand cmd = new SqlCommand(getColumnsQuery, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("\nColumns in Users table:");
                                Console.WriteLine("------------------------------------------------");
                                Console.WriteLine("Column Name       | Data Type       | Max Length");
                                Console.WriteLine("------------------------------------------------");
                                
                                while (reader.Read())
                                {
                                    string columnName = reader["COLUMN_NAME"].ToString();
                                    string dataType = reader["DATA_TYPE"].ToString();
                                    string maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 
                                                      "N/A" : reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                                    
                                    Console.WriteLine($"{columnName.PadRight(18)} | {dataType.PadRight(15)} | {maxLength}");
                                }
                                Console.WriteLine("------------------------------------------------");
                            }
                            else
                            {
                                Console.WriteLine("No columns found in Users table.");
                            }
                        }
                    }

                    // Check user roles table
                    string checkRoleTableQuery = @"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserRoles')
                            SELECT 'Table exists' as Status
                        ELSE
                            SELECT 'Table does not exist' as Status";

                    using (SqlCommand cmd = new SqlCommand(checkRoleTableQuery, connection))
                    {
                        string tableStatus = cmd.ExecuteScalar().ToString();
                        Console.WriteLine($"\nUserRoles table status: {tableStatus}");
                    }

                    // If UserRoles table exists, check its structure
                    string getRoleColumnsQuery = @"
                        SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'UserRoles'
                        ORDER BY ORDINAL_POSITION";

                    using (SqlCommand cmd = new SqlCommand(getRoleColumnsQuery, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("\nColumns in UserRoles table:");
                                Console.WriteLine("------------------------------------------------");
                                Console.WriteLine("Column Name       | Data Type       | Max Length");
                                Console.WriteLine("------------------------------------------------");
                                
                                while (reader.Read())
                                {
                                    string columnName = reader["COLUMN_NAME"].ToString();
                                    string dataType = reader["DATA_TYPE"].ToString();
                                    string maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 
                                                      "N/A" : reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                                    
                                    Console.WriteLine($"{columnName.PadRight(18)} | {dataType.PadRight(15)} | {maxLength}");
                                }
                                Console.WriteLine("------------------------------------------------");
                            }
                            else
                            {
                                Console.WriteLine("No columns found in UserRoles table.");
                            }
                        }
                    }

                    // Check for admin user
                    string checkAdminQuery = @"
                        IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
                            SELECT 'Admin user exists' as Status
                        ELSE
                            SELECT 'Admin user does not exist' as Status";

                    using (SqlCommand cmd = new SqlCommand(checkAdminQuery, connection))
                    {
                        try
                        {
                            string adminStatus = cmd.ExecuteScalar().ToString();
                            Console.WriteLine($"\nAdmin user status: {adminStatus}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nCould not check admin user status: {ex.Message}");
                        }
                    }

                    // Get all users
                    string getUsersQuery = @"SELECT * FROM Users";
                    using (SqlCommand cmd = new SqlCommand(getUsersQuery, connection))
                    {
                        try
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    Console.WriteLine("\nUsers in database:");
                                    Console.WriteLine("------------------------------------------------");
                                    
                                    // Get column names
                                    int fieldCount = reader.FieldCount;
                                    string[] columnNames = new string[fieldCount];
                                    for (int i = 0; i < fieldCount; i++)
                                    {
                                        columnNames[i] = reader.GetName(i);
                                    }
                                    
                                    // Print column names
                                    Console.WriteLine(string.Join(" | ", columnNames));
                                    Console.WriteLine("------------------------------------------------");
                                    
                                    // Print data
                                    while (reader.Read())
                                    {
                                        string[] values = new string[fieldCount];
                                        for (int i = 0; i < fieldCount; i++)
                                        {
                                            values[i] = reader[i].ToString();
                                        }
                                        Console.WriteLine(string.Join(" | ", values));
                                    }
                                    
                                    Console.WriteLine("------------------------------------------------");
                                }
                                else
                                {
                                    Console.WriteLine("\nNo users found in the database.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nCould not retrieve users: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to the database: {ex.Message}");
                }
            }
        }
    }
}
