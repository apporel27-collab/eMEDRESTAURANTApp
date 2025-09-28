using System;
using System.Data.SqlClient;

namespace FixPhoneColumn
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=5;";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to the database successfully.");

                    // Check if Phone column exists
                    bool phoneExists = false;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Phone'", connection))
                    {
                        phoneExists = ((int)cmd.ExecuteScalar() > 0);
                    }
                    
                    if (!phoneExists)
                    {
                        // Add the Phone column
                        using (var cmd = new SqlCommand(
                            "ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(20) NULL", connection))
                        {
                            cmd.ExecuteNonQuery();
                            Console.WriteLine("Added Phone column to Users table successfully!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Phone column already exists in the Users table.");
                    }

                    // Verify the column was added
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Phone'", connection))
                    {
                        bool verifyPhoneExists = ((int)cmd.ExecuteScalar() > 0);
                        Console.WriteLine($"Phone column exists check: {verifyPhoneExists}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
