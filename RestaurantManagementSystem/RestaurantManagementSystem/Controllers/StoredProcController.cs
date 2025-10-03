using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text;

namespace RestaurantManagementSystem.Controllers
{
    public class StoredProcController : Controller
    {
        private readonly string _connectionString;

        public StoredProcController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: StoredProc/UpdateGetAllMenuItemsProc
        public IActionResult UpdateGetAllMenuItemsProc()
        {
            try
            {
                // Define the new stored procedure SQL inline
                string sql = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllMenuItems]') AND type in (N'P'))
                    DROP PROCEDURE [dbo].[sp_GetAllMenuItems]
                
                CREATE PROCEDURE [dbo].[sp_GetAllMenuItems]
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    SELECT 
                        m.[Id], 
                        ISNULL(m.[PLUCode], '') AS PLUCode,
                        m.[Name], 
                        m.[Description], 
                        m.[Price], 
                        m.[CategoryId], 
                        c.[Name] AS CategoryName,
                        m.[ImagePath], 
                        m.[IsAvailable], 
                        ISNULL(m.[PrepTime], 0) AS PreparationTimeMinutes,
                        m.[CalorieCount],
                        ISNULL(m.[IsFeatured], 0) AS IsFeatured,
                        ISNULL(m.[IsSpecial], 0) AS IsSpecial,
                        m.[DiscountPercentage],
                        m.[KitchenStationId],
                        m.[TargetGP]
                    FROM [dbo].[MenuItems] m
                    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
                    ORDER BY m.[Name];
                END";
                
                // Execute the SQL
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                
                StringBuilder result = new StringBuilder();
                result.AppendLine("Updated sp_GetAllMenuItems stored procedure successfully!");
                
                // Now update all Helper methods in DatabaseHelper class to handle missing columns gracefully
                return Content(result.ToString());
            }
            catch (Exception ex)
            {
                return Content($"Error updating stored procedure: {ex.Message}");
            }
        }
    }
}
