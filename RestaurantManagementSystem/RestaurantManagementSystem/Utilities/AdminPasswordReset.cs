using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagementSystem.Utilities;
using BCrypt.Net;

namespace RestaurantManagementSystem.Utilities
{
    public class AdminPasswordReset
    {
        public static async Task ResetAdminPassword(IServiceProvider serviceProvider)
        {
            try
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AdminPasswordReset>>();
                var passwordResetTool = serviceProvider.GetRequiredService<PasswordResetTool>();
                
                // Check connection
                bool connectionOk = await passwordResetTool.CheckConnection();
                if (!connectionOk)
                {
                    logger.LogError("Database connection failed. Cannot reset admin password.");
                    return;
                }
                
                // Get current admin password hash
                string currentHash = await passwordResetTool.GetUserPasswordHash("admin");
                logger.LogInformation("Current admin password hash: {Hash}", currentHash);
                
                // Reset the admin password to "Admin@123"
                bool success = await passwordResetTool.ResetUserPassword("admin", "Admin@123");
                
                if (success)
                {
                    logger.LogInformation("Successfully reset admin password to 'Admin@123'");
                    
                    // Verify the new password
                    string newHash = await passwordResetTool.GetUserPasswordHash("admin");
                    logger.LogInformation("New admin password hash: {Hash}", newHash);
                    
                    bool isVerified = await passwordResetTool.TestPasswordVerification("admin", "Admin@123");
                    logger.LogInformation("Password verification test: {Result}", isVerified ? "SUCCESS" : "FAILED");
                }
                else
                {
                    logger.LogError("Failed to reset admin password.");
                }
            }
            catch (Exception ex)
            {
                
                
            }
        }
    }
}
