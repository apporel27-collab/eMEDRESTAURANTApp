using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace AuthDiagnostic
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;";

            Console.WriteLine("=== Authentication Diagnostic Tool ===");
            Console.WriteLine("Checking database connection...");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Database connection successful.");

                    // Check Users table structure
                    Console.WriteLine("\n=== Users Table Structure ===");
                    using (var command = new SqlCommand(@"
                        SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Users'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("Column Name".PadRight(25) + "Data Type".PadRight(15) + "Max Length");
                                Console.WriteLine("----------------------------------------------------");
                                while (reader.Read())
                                {
                                    string columnName = reader["COLUMN_NAME"].ToString() ?? "";
                                    string dataType = reader["DATA_TYPE"].ToString() ?? "";
                                    string maxLength = reader["CHARACTER_MAXIMUM_LENGTH"]?.ToString() ?? "N/A";
                                    
                                    Console.WriteLine($"{columnName.PadRight(25)}{dataType.PadRight(15)}{maxLength}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No columns found in Users table. Table might not exist.");
                            }
                        }
                    }

                    // Check admin user details
                    Console.WriteLine("\n=== Admin User Details ===");
                    using (var command = new SqlCommand("SELECT * FROM Users WHERE Username = 'admin'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Console.WriteLine("Admin User Found:");
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i);
                                        string value = reader[i]?.ToString() ?? "NULL";
                                        Console.WriteLine($"  {columnName}: {value}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Admin user not found in database.");
                            }
                        }
                    }

                    // Verify if PasswordHash column exists
                    Console.WriteLine("\n=== Password Hash Column Check ===");
                    bool hasPasswordHash = false;
                    bool hasPassword = false;
                    bool hasSalt = false;
                    
                    using (var command = new SqlCommand(@"
                        SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Users' AND 
                        COLUMN_NAME IN ('PasswordHash', 'Password', 'Salt')", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader["COLUMN_NAME"].ToString() ?? "";
                                if (columnName == "PasswordHash") hasPasswordHash = true;
                                if (columnName == "Password") hasPassword = true;
                                if (columnName == "Salt") hasSalt = true;
                                
                                Console.WriteLine($"Found column: {columnName}");
                            }
                        }
                    }
                    
                    // Update schema if needed
                    if (!hasPasswordHash && hasPassword)
                    {
                        Console.WriteLine("\nNeed to rename Password column to PasswordHash...");
                        try
                        {
                            using (var command = new SqlCommand("EXEC sp_rename 'Users.Password', 'PasswordHash', 'COLUMN'", connection))
                            {
                                command.ExecuteNonQuery();
                                Console.WriteLine("Successfully renamed Password column to PasswordHash");
                                hasPasswordHash = true;
                                hasPassword = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error renaming column: {ex.Message}");
                        }
                    }
                    
                    if (!hasSalt)
                    {
                        Console.WriteLine("\nNeed to add Salt column...");
                        try
                        {
                            using (var command = new SqlCommand("ALTER TABLE Users ADD Salt NVARCHAR(100)", connection))
                            {
                                command.ExecuteNonQuery();
                                Console.WriteLine("Successfully added Salt column");
                                hasSalt = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error adding Salt column: {ex.Message}");
                        }
                    }
                    
                    // Update admin user with proper hash
                    if (hasPasswordHash && hasSalt)
                    {
                        Console.WriteLine("\n=== Updating Admin User ===");
                        string salt = "abc123";
                        string password = "password";
                        string passwordHash = HashPassword(password, salt);
                        
                        Console.WriteLine($"Generated hash: {passwordHash}");
                        
                        try
                        {
                            using (var command = new SqlCommand(@"
                                UPDATE Users 
                                SET PasswordHash = @PasswordHash, 
                                    Salt = @Salt,
                                    IsLockedOut = 0,
                                    FailedLoginAttempts = 0
                                WHERE Username = 'admin'", connection))
                            {
                                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                                command.Parameters.AddWithValue("@Salt", salt);
                                int rowsAffected = command.ExecuteNonQuery();
                                Console.WriteLine($"Admin user updated: {rowsAffected} rows affected");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating admin user: {ex.Message}");
                        }
                    }
                    
                    // Verify the update
                    Console.WriteLine("\n=== Verifying Admin User After Update ===");
                    using (var command = new SqlCommand("SELECT * FROM Users WHERE Username = 'admin'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Console.WriteLine("Admin User Details:");
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i);
                                        string value = reader[i]?.ToString() ?? "NULL";
                                        Console.WriteLine($"  {columnName}: {value}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Admin user not found after update. This indicates a problem.");
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
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine("\nDiagnostic complete. Press any key to exit.");
            Console.ReadKey();
        }

        private static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, 
                Encoding.UTF8.GetBytes(salt), 
                1000, 
                HashAlgorithmName.SHA256))
            {
                var hashBytes = pbkdf2.GetBytes(32);
                return $"1000:{salt}:{Convert.ToHexString(hashBytes).ToLower()}";
            }
        }
    }
}
