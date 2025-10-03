using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

class Program
{
    private static string _connectionString = "Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=5;";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Installing Home Dashboard stored procedures...");
        
        try
        {
            await InstallStoredProcedure("usp_GetHomeDashboardStats.sql");
            await InstallStoredProcedure("usp_GetRecentOrdersForDashboard.sql");
            
            Console.WriteLine("All stored procedures installed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing stored procedures: {ex.Message}");
        }
    }

    private static async Task InstallStoredProcedure(string fileName)
    {
        try
        {
            string filePath = Path.Combine("RestaurantManagementSystem", "RestaurantManagementSystem", "SQL", fileName);
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            string sqlScript = await File.ReadAllTextAsync(filePath);
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                Console.WriteLine($"Installing {fileName}...");
                
                using (var command = new SqlCommand(sqlScript, connection))
                {
                    command.CommandTimeout = 300; // 5 minutes timeout
                    await command.ExecuteNonQueryAsync();
                }
                
                Console.WriteLine($"✓ {fileName} installed successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error installing {fileName}: {ex.Message}");
            throw;
        }
    }
}