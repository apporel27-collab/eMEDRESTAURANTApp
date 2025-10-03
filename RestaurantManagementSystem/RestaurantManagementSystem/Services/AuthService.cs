using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        
        public AuthService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger = null)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        
        public async Task<(bool Success, string Message, ClaimsPrincipal Principal)> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                _logger?.LogInformation("Attempting to authenticate user: {Username}", username);
                
                // Debug log for connection string
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                _logger?.LogInformation("Using connection string: {ConnectionString}", 
                    connectionString?.Substring(0, Math.Min(connectionString?.Length ?? 0, 30)) + "..." ?? "NULL");
                
                var user = await GetUserByUsernameAsync(username);
            
                if (user == null)
                {
                    _logger?.LogWarning("Authentication failed: User not found: {Username}", username);
                    return (false, "Invalid username or password", null);
                }
                
                _logger?.LogInformation("User found with ID: {UserId}, Username: {Username}, Hash: {PasswordHashLength} chars", 
                    user.Id, user.Username, user.PasswordHash?.Length ?? 0);
                    
                try {
                    bool passwordVerified = VerifyPassword(password, user.PasswordHash);
                    _logger?.LogInformation("Password verification result: {Result}", passwordVerified);
                    
                    if (!passwordVerified)
                    {
                        _logger?.LogWarning("Authentication failed: Invalid password for user: {Username}", username);
                        // Increment failed login attempts
                        await IncrementFailedLoginAttemptsAsync(user.Id);
                        return (false, "Invalid username or password", null);
                    }
                } catch (Exception pwEx) {
                    _logger?.LogError(pwEx, "Error during password verification for user {Username}", username);
                    return (false, "An error occurred during password verification", null);
                }
                
                if (user.IsLockedOut)
                {
                    return (false, "Your account is locked out. Please contact an administrator.", null);
                }
                
                if (!user.IsActive)
                {
                    return (false, "Your account is not active. Please contact an administrator.", null);
                }
                
                // Reset failed login attempts
                await ResetFailedLoginAttemptsAsync(user.Id);
                
                // Get user roles
                user.Roles = await GetUserRolesAsync(user.Id);
                
                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("FullName", $"{user.FirstName} {user.LastName}".Trim()),
                    new Claim(ClaimTypes.GivenName, user.FirstName)
                };
                
                // Add email if available
                if (!string.IsNullOrEmpty(user.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, user.Email));
                }
                
                // Add surname if available
                if (!string.IsNullOrEmpty(user.LastName))
                {
                    claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
                }
                
                // Add MFA claim if applicable
                if (user.RequiresMFA)
                {
                    claims.Add(new Claim("RequiresMFA", "true"));
                }
                
                // Add user roles to claims
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Name));
                    _logger?.LogInformation("Added role claim: {Role} for user: {Username}", role.Name, username);
                }
                
                // Create identity and principal
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                
                // Update last login time
                await UpdateLastLoginTimeAsync(user.Id);
                
                return (true, "Authentication successful", principal);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during authentication process for user: {Username}", username);
                return (false, "An error occurred during authentication", null);
            }
        }
        
        public async Task SignInUserAsync(ClaimsPrincipal principal, bool rememberMe)
        {
            // Sign in the user
            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                });
        }

        public async Task SignOutUserAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        
        private async Task<User> GetUserByUsernameAsync(string username)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    _logger?.LogInformation("Database connection opened successfully");
                    
                    using (var command = new SqlCommand("SELECT * FROM Users WHERE Username = @Username", connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        _logger?.LogInformation("Executing query for username: {Username}", username);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                _logger?.LogInformation("User found in database");
                                
                                // Debug log all columns to help diagnose issues
                                var columnNames = new List<string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    columnNames.Add(reader.GetName(i));
                                }
                                _logger?.LogInformation("Columns in Users table: {Columns}", string.Join(", ", columnNames));
                                
                                // Extract the password hash directly to check its format
                                string passwordHash = reader["PasswordHash"]?.ToString();
                                _logger?.LogInformation("Raw PasswordHash: {PasswordHash}", passwordHash ?? "NULL");
                                
                                return new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"].ToString(),
                                    PasswordHash = passwordHash,
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader["LastName"].ToString(),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader["Email"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    IsLockedOut = Convert.ToBoolean(reader["IsLockedOut"]),
                                    RequiresMFA = reader.IsDBNull(reader.GetOrdinal("RequiresMFA")) ? false : Convert.ToBoolean(reader["RequiresMFA"]),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedDate"])
                                };
                            }
                            else
                            {
                                _logger?.LogWarning("No user found with username: {Username}", username);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error retrieving user from database");
                }
            }
            
            return null;
        }
        
        private async Task<List<Role>> GetUserRolesAsync(int userId)
        {
            var roles = new List<Role>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(@"
                        SELECT r.Id, r.Name, r.Description
                        FROM Roles r
                        INNER JOIN UserRoles ur ON r.Id = ur.RoleId
                        WHERE ur.UserId = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                roles.Add(new Role
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user roles");
            }
            
            return roles;
        }
        
        public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(@"
                        SELECT COUNT(1)
                        FROM UserRoles ur
                        INNER JOIN Roles r ON ur.RoleId = r.Id
                        WHERE ur.UserId = @UserId AND r.Name = @RoleName", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@RoleName", roleName);
                        
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if user is in role");
                return false;
            }
        }
        
        public async Task<(bool success, string message)> RegisterUserAsync(User user, string password, string roleName = "Staff")
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                // Hash the password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Begin transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert user
                            int userId;
                            using (var command = new SqlCommand(@"
                                INSERT INTO Users (Username, PasswordHash, FirstName, LastName, Email, IsActive, IsLockedOut, RequiresMFA, CreatedAt)
                                VALUES (@Username, @PasswordHash, @FirstName, @LastName, @Email, @IsActive, @IsLockedOut, @RequiresMFA, @CreatedAt);
                                SELECT SCOPE_IDENTITY();", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Username", user.Username);
                                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                                command.Parameters.AddWithValue("@LastName", user.LastName ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Email", user.Email ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@IsActive", user.IsActive);
                                command.Parameters.AddWithValue("@IsLockedOut", user.IsLockedOut);
                                command.Parameters.AddWithValue("@RequiresMFA", user.RequiresMFA);
                                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                                
                                userId = Convert.ToInt32(await command.ExecuteScalarAsync());
                            }
                            
                            // Get role ID
                            int roleId;
                            using (var command = new SqlCommand("SELECT Id FROM Roles WHERE Name = @RoleName", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@RoleName", roleName);
                                var result = await command.ExecuteScalarAsync();
                                
                                if (result == null)
                                {
                                    // Role doesn't exist, create it
                                    using (var createRoleCommand = new SqlCommand("INSERT INTO Roles (Name, Description) VALUES (@Name, @Description); SELECT SCOPE_IDENTITY();", connection, transaction))
                                    {
                                        createRoleCommand.Parameters.AddWithValue("@Name", roleName);
                                        createRoleCommand.Parameters.AddWithValue("@Description", $"{roleName} role");
                                        roleId = Convert.ToInt32(await createRoleCommand.ExecuteScalarAsync());
                                    }
                                }
                                else
                                {
                                    roleId = Convert.ToInt32(result);
                                }
                            }
                            
                            // Assign role to user
                            using (var command = new SqlCommand("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@RoleId", roleId);
                                await command.ExecuteNonQueryAsync();
                            }
                            
                            // Commit transaction
                            transaction.Commit();
                            return (true, "User registered successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error registering user");
                            transaction.Rollback();
                            return (false, $"Error registering user: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error registering user");
                return (false, $"Error registering user: {ex.Message}");
            }
        }
        
        public async Task<(bool success, string message)> UpdatePasswordAsync(int userId, string newPassword)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                // Hash the new password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0 
                            ? (true, "Password updated successfully") 
                            : (false, "User not found");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating password");
                return (false, $"Error updating password: {ex.Message}");
            }
        }
        
        public async Task<(bool success, string message)> LockUserAsync(int userId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("UPDATE Users SET IsLockedOut = 1 WHERE Id = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0
                            ? (true, "User locked successfully")
                            : (false, "User not found");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error locking user");
                return (false, $"Error locking user: {ex.Message}");
            }
        }
        
        public async Task<(bool success, string message)> UnlockUserAsync(int userId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("UPDATE Users SET IsLockedOut = 0 WHERE Id = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0
                            ? (true, "User unlocked successfully")
                            : (false, "User not found");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unlocking user");
                return (false, $"Error unlocking user: {ex.Message}");
            }
        }
        
        public async Task<(bool success, string message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get current password hash
                    string currentHash;
                    using (var command = new SqlCommand("SELECT PasswordHash FROM Users WHERE Id = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        currentHash = (string)await command.ExecuteScalarAsync();
                    }
                    
                    // Verify current password
                    if (!VerifyPassword(currentPassword, currentHash))
                    {
                        return (false, "Current password is incorrect");
                    }
                    
                    // Update to new password
                    string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
                    
                    using (var command = new SqlCommand("UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@PasswordHash", newHash);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    return (true, "Password changed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error changing password");
                return (false, "An error occurred while changing the password");
            }
        }
        
        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("SELECT * FROM Users ORDER BY Username", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                users.Add(new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader["LastName"].ToString(),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader["Email"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    IsLockedOut = Convert.ToBoolean(reader["IsLockedOut"]),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedDate"])
                                });
                            }
                        }
                    }
                    
                    // Get roles for each user
                    foreach (var user in users)
                    {
                        user.Roles = await GetUserRolesAsync(user.Id);
                    }
                }
                
                return users;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting users");
                return new List<User>();
            }
        }
        
        public async Task<User> GetUserForEditAsync(int userId)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("SELECT * FROM Users WHERE Id = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var user = new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader["LastName"].ToString(),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader["Email"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    IsLockedOut = Convert.ToBoolean(reader["IsLockedOut"]),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedDate"])
                                };
                                
                                // Get user roles
                                user.Roles = await GetUserRolesAsync(user.Id);
                                
                                return user;
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user for edit");
                return null;
            }
        }
        
        public async Task<(bool success, string message)> UpdateUserAsync(User user, int updatedByUserId)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update user
                            using (var command = new SqlCommand(@"
                                UPDATE Users
                                SET FirstName = @FirstName,
                                    LastName = @LastName,
                                    Email = @Email,
                                    IsActive = @IsActive,
                                    IsLockedOut = @IsLockedOut
                                WHERE Id = @UserId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", user.Id);
                                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                                command.Parameters.AddWithValue("@LastName", user.LastName ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Email", user.Email ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@IsActive", user.IsActive);
                                command.Parameters.AddWithValue("@IsLockedOut", user.IsLockedOut);
                                
                                await command.ExecuteNonQueryAsync();
                            }
                            
                            // Update user roles (remove all and add selected)
                            using (var command = new SqlCommand("DELETE FROM UserRoles WHERE UserId = @UserId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", user.Id);
                                await command.ExecuteNonQueryAsync();
                            }
                            
                            if (user.Roles != null && user.Roles.Count > 0)
                            {
                                foreach (var roleName in user.Roles)
                                {
                                    // Get role ID
                                    int roleId;
                                    using (var command = new SqlCommand("SELECT Id FROM Roles WHERE Name = @RoleName", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@RoleName", roleName);
                                        var result = await command.ExecuteScalarAsync();
                                        
                                        if (result == null)
                                        {
                                            // Role doesn't exist, create it
                                            using (var createRoleCommand = new SqlCommand("INSERT INTO Roles (Name, Description) VALUES (@Name, @Description); SELECT SCOPE_IDENTITY();", connection, transaction))
                                            {
                                                createRoleCommand.Parameters.AddWithValue("@Name", roleName);
                                                createRoleCommand.Parameters.AddWithValue("@Description", $"{roleName} role");
                                                roleId = Convert.ToInt32(await createRoleCommand.ExecuteScalarAsync());
                                            }
                                        }
                                        else
                                        {
                                            roleId = Convert.ToInt32(result);
                                        }
                                    }
                                    
                                    // Assign role to user
                                    using (var command = new SqlCommand("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@UserId", user.Id);
                                        command.Parameters.AddWithValue("@RoleId", roleId);
                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            
                            transaction.Commit();
                            return (true, "User updated successfully");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger?.LogError(ex, "Error updating user");
                            return (false, "An error occurred while updating the user");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user");
                return (false, "An error occurred while updating the user");
            }
        }
        
        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                _logger?.LogInformation("Verifying password with BCrypt. Hash length: {Length}", passwordHash?.Length ?? 0);
                
                // Check if hash is in BCrypt format
                bool isBcryptFormat = passwordHash != null && (passwordHash.StartsWith("$2a$") || passwordHash.StartsWith("$2b$") || passwordHash.StartsWith("$2y$"));
                _logger?.LogInformation("Hash is in BCrypt format: {IsBcryptFormat}", isBcryptFormat);
                
                if (!isBcryptFormat)
                {
                    _logger?.LogWarning("Password hash is not in BCrypt format");
                    return false;
                }
                
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error verifying password with BCrypt");
                throw;
            }
        }
        
        private async Task IncrementFailedLoginAttemptsAsync(int userId)
        {
            // This would update a failed login attempts counter in your database
            // For now, just logging it
            _logger?.LogInformation("Incrementing failed login attempts for user ID: {UserId}", userId);
        }
        
        private async Task ResetFailedLoginAttemptsAsync(int userId)
        {
            // This would reset a failed login attempts counter in your database
            // For now, just logging it
            _logger?.LogInformation("Resetting failed login attempts for user ID: {UserId}", userId);
        }
        
        private async Task UpdateLastLoginTimeAsync(int userId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                                  WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastLoginAt')
                        BEGIN
                            UPDATE Users SET LastLoginAt = @LastLoginAt WHERE Id = @UserId
                        END", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@LastLoginAt", DateTime.UtcNow);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating last login time: {Error}", ex.Message);
            }
        }
    }
}
