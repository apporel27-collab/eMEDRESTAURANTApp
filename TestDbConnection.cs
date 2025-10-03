using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

class TestDbConnection
{
    static async Task Main(string[] args)
    {
        string connectionString = "Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=5;";
        
        Console.WriteLine("Testing database connection...");
        
        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                Console.WriteLine("Attempting to connect to database...");
                await connection.OpenAsync();
                Console.WriteLine("✅ Database connection successful!");
                
                using (var command = new SqlCommand("SELECT @@VERSION", connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    Console.WriteLine($"Database version: {result}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database connection failed: {ex.Message}");
            Console.WriteLine($"Full error: {ex}");
        }
    }
}