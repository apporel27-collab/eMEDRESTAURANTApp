using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;";

        Console.WriteLine("=== Disabling MFA for Admin User ===");
        
        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Database connection successful.");
                
                // Update admin user with MFA disabled
                using (var command = new SqlCommand(@"
                    UPDATE Users 
                    SET RequiresMFA = 0
                    WHERE Username = 'admin'", connection))
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Admin user updated: {rowsAffected} rows affected");
                }
                
                // Verify the update
                Console.WriteLine("\n=== Verifying Admin User After Update ===");
                using (var command = new SqlCommand("SELECT Id, Username, Email, RequiresMFA FROM Users WHERE Username = 'admin'", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("Admin User Details:");
                                Console.WriteLine($"  Id: {reader["Id"]}");
                                Console.WriteLine($"  Username: {reader["Username"]}");
                                Console.WriteLine($"  Email: {reader["Email"]}");
                                Console.WriteLine($"  RequiresMFA: {reader["RequiresMFA"]}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Admin user not found after update. This indicates a problem.");
                        }
                    }
                }
            }

            Console.WriteLine("\nMFA disabled successfully for admin user.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
            }
        }
    }
}
