using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

class AdminDiagnostic
{
    static void Main(string[] args)
    {
        try
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine("Using connection string: " + connectionString);

            // Test database connection
            using (var connection = new SqlConnection(connectionString))
            {
                Console.WriteLine("Opening database connection...");
                connection.Open();
                Console.WriteLine("Connected to database!");

                // Check if Users table exists
                Console.WriteLine("\nChecking Users table...");
                bool usersTableExists = CheckIfTableExists(connection, "Users");
                Console.WriteLine("Users table exists: " + usersTableExists);

                if (usersTableExists)
                {
                    // Get Users table schema
                    Console.WriteLine("\nUsers table schema:");
                    using (var command = new SqlCommand(@"
                        SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'Users'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader["COLUMN_NAME"].ToString();
                                string dataType = reader["DATA_TYPE"].ToString();
                                string length = reader["CHARACTER_MAXIMUM_LENGTH"]?.ToString() ?? "NULL";
                                Console.WriteLine($"- {columnName} ({dataType}({length}))");
                            }
                        }
                    }

                    // Check if admin user exists
                    Console.WriteLine("\nChecking admin user...");
                    bool adminExists = CheckIfAdminExists(connection);
                    Console.WriteLine("Admin user exists: " + adminExists);

                    if (adminExists)
                    {
                        // Get admin user details
                        Console.WriteLine("\nAdmin user details:");
                        DisplayAdminDetails(connection);

                        // Attempt to manually create a hash
                        Console.WriteLine("\nGenerating password hash for 'password':");
                        string password = "password";
                        string salt = "abc123";
                        string hash = HashPassword(password, salt);
                        Console.WriteLine($"Password hash: {hash}");

                        // Update admin user with new hash
                        Console.WriteLine("\nUpdating admin user with new hash...");
                        UpdateAdminUser(connection, hash, salt);
                        Console.WriteLine("Admin user updated!");

                        // Display updated admin details
                        Console.WriteLine("\nUpdated admin user details:");
                        DisplayAdminDetails(connection);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
            }
            Console.WriteLine(ex.StackTrace);
        }
    }

    static bool CheckIfTableExists(SqlConnection connection, string tableName)
    {
        using (var command = new SqlCommand(@"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = @TableName", connection))
        {
            command.Parameters.AddWithValue("@TableName", tableName);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    static bool CheckIfAdminExists(SqlConnection connection)
    {
        using (var command = new SqlCommand(@"
            SELECT COUNT(*) 
            FROM Users
            WHERE Username = 'admin'", connection))
        {
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    static void DisplayAdminDetails(SqlConnection connection)
    {
        try
        {
            using (var command = new SqlCommand(@"
                SELECT *
                FROM Users
                WHERE Username = 'admin'", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            string value = reader[i]?.ToString() ?? "NULL";
                            Console.WriteLine($"- {columnName}: {value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No admin user found!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error displaying admin details: {ex.Message}");
        }
    }

    static void UpdateAdminUser(SqlConnection connection, string hash, string salt)
    {
        // First, determine column names
        bool hasPasswordHash = false;
        bool hasPassword = false;

        using (var command = new SqlCommand(@"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'Users'", connection))
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string columnName = reader["COLUMN_NAME"].ToString();
                    if (columnName.Equals("PasswordHash", StringComparison.OrdinalIgnoreCase))
                        hasPasswordHash = true;
                    if (columnName.Equals("Password", StringComparison.OrdinalIgnoreCase))
                        hasPassword = true;
                }
            }
        }

        // Construct update SQL based on existing columns
        string passwordColumn = hasPasswordHash ? "PasswordHash" : "Password";
        string sql = $@"
            UPDATE Users
            SET {passwordColumn} = @Hash,
                Salt = @Salt,
                IsLockedOut = 0,
                FailedLoginAttempts = 0
            WHERE Username = 'admin'";

        using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@Hash", hash);
            command.Parameters.AddWithValue("@Salt", salt);
            int rowsAffected = command.ExecuteNonQuery();
            Console.WriteLine($"Rows updated: {rowsAffected}");
        }
    }

    static string HashPassword(string password, string salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 1000, HashAlgorithmName.SHA256))
        {
            var hashBytes = pbkdf2.GetBytes(32);
            return $"1000:{salt}:{Convert.ToHexString(hashBytes).ToLower()}";
        }
    }
}
