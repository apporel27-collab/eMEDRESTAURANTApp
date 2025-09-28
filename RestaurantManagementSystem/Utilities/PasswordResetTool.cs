using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace RestaurantManagementSystem.Utilities
{
    public class PasswordResetTool
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordResetTool> _logger;

        public PasswordResetTool(IConfiguration configuration, ILogger<PasswordResetTool> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> CheckConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger.LogInformation("Checking database connection with string: {ConnectionString}", connectionString);
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed");
                return false;
            }
        }

        public async Task<string> GetUserPasswordHash(string username)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("SELECT PasswordHash FROM Users WHERE Username = @Username", connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        
                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            string hash = result.ToString();
                            _logger.LogInformation("Retrieved hash for user {Username}: {Hash}", username, hash);
                            _logger.LogInformation("Hash length: {Length}", hash.Length);
                            _logger.LogInformation("Hash starts with: {Prefix}", hash.Length > 10 ? hash.Substring(0, 10) : hash);
                            
                            // Check if it's a valid BCrypt hash
                            bool isBcryptFormat = hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$");
                            _logger.LogInformation("Is BCrypt format: {IsBcrypt}", isBcryptFormat);
                            
                            return hash;
                        }
                        
                        _logger.LogWarning("No password hash found for user {Username}", username);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving password hash");
                return null;
            }
        }

        public async Task<bool> ResetUserPassword(string username, string newPassword)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                // Generate a proper BCrypt hash
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
                
                _logger.LogInformation("Generated new hash: {Hash}", passwordHash);
                _logger.LogInformation("Hash length: {Length}", passwordHash.Length);
                _logger.LogInformation("Hash starts with: {Prefix}", passwordHash.Substring(0, 10));
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("UPDATE Users SET PasswordHash = @PasswordHash WHERE Username = @Username", connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            _logger.LogInformation("Successfully reset password for user {Username}", username);
                            return true;
                        }
                        
                        _logger.LogWarning("No user found with username {Username}", username);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return false;
            }
        }

        public async Task<bool> TestPasswordVerification(string username, string password)
        {
            var hash = await GetUserPasswordHash(username);
            if (string.IsNullOrEmpty(hash))
            {
                _logger.LogWarning("No hash found for user {Username}", username);
                return false;
            }

            try
            {
                _logger.LogInformation("Attempting to verify password with BCrypt");
                bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
                _logger.LogInformation("Password verification result: {Result}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password with BCrypt");
                
                // If there was an error, the hash might not be in the expected format
                _logger.LogInformation("Hash format may be incorrect. Let's reset the password");
                return false;
            }
        }
    }
}
