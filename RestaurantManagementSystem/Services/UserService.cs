using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Services
{
    public class UserService
    {
        private readonly string _connectionString;
        
        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        /// <summary>
        /// Gets all users from the database with their roles
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_GetUsersList", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var user = new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"].ToString(),
                                    FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                                    LastName = reader["LastName"]?.ToString() ?? string.Empty,
                                    Email = reader["Email"]?.ToString(),
                                    Phone = reader["Phone"]?.ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    IsLockedOut = Convert.ToBoolean(reader["IsLockedOut"]),
                                    LastLoginDate = reader["LastLoginDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["LastLoginDate"])
                                        : null
                                };
                                
                                // Add role names (comma-separated string)
                                string roleNames = reader["Roles"]?.ToString();
                                if (!string.IsNullOrEmpty(roleNames))
                                {
                                    // We're not setting actual Role objects here since we only have names
                                    // This is just for display purposes in the UI
                                }
                                
                                users.Add(user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting users: {ex.Message}");
                throw;
            }
            
            return users;
        }
        
        /// <summary>
        /// Gets a user by their ID, including their roles
        /// </summary>
        public async Task<User> GetUserByIdAsync(int id)
        {
            User user = null;
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_GetUserById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", id);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"].ToString(),
                                    FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                                    LastName = reader["LastName"]?.ToString() ?? string.Empty,
                                    Email = reader["Email"]?.ToString(),
                                    Phone = reader["Phone"]?.ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    IsLockedOut = Convert.ToBoolean(reader["IsLockedOut"]),
                                    LastLoginDate = reader["LastLoginDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["LastLoginDate"])
                                        : null
                                };
                            }
                            
                            // If we have a second result set with roles
                            if (user != null && await reader.NextResultAsync())
                            {
                                var roleIds = new List<int>();
                                
                                while (await reader.ReadAsync())
                                {
                                    roleIds.Add(Convert.ToInt32(reader["Id"]));
                                }
                                
                                user.SelectedRoleIds = roleIds;
                            }
                        }
                    }
                }
                
                return user;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting user by ID: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Creates a new user
        /// </summary>
        public async Task<bool> CreateUserAsync(User user, int createdBy)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Generate salt and hash password
                    var (salt, passwordHash) = PasswordHasher.HashPassword(user.Password);
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_CreateUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        command.Parameters.AddWithValue("@Salt", salt);
                        command.Parameters.AddWithValue("@FirstName", user.FirstName);
                        command.Parameters.AddWithValue("@LastName", user.LastName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", user.Email ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsActive", user.IsActive);
                        command.Parameters.AddWithValue("@IsLockedOut", user.IsLockedOut);
                        command.Parameters.AddWithValue("@RequiresMFA", user.RequiresMFA);
                        command.Parameters.AddWithValue("@MustChangePassword", user.MustChangePassword);
                        command.Parameters.AddWithValue("@CreatedBy", createdBy == 0 ? (object)DBNull.Value : createdBy);
                        
                        var outputParameter = command.Parameters.Add("@UserId", SqlDbType.Int);
                        outputParameter.Direction = ParameterDirection.Output;
                        
                        await command.ExecuteNonQueryAsync();
                        
                        // Get the new user ID
                        user.Id = (int)outputParameter.Value;
                        
                        return user.Id > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates an existing user
        /// </summary>
        public async Task<bool> UpdateUserAsync(User user, int updatedBy)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_UpdateUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@UserId", user.Id);
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@FirstName", user.FirstName);
                        command.Parameters.AddWithValue("@LastName", user.LastName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", user.Email ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsActive", user.IsActive);
                        command.Parameters.AddWithValue("@IsLockedOut", user.IsLockedOut);
                        command.Parameters.AddWithValue("@RequiresMFA", user.RequiresMFA);
                        command.Parameters.AddWithValue("@MustChangePassword", user.MustChangePassword);
                        command.Parameters.AddWithValue("@UpdatedBy", updatedBy == 0 ? (object)DBNull.Value : updatedBy);
                        
                        // If password is provided, update it
                        if (!string.IsNullOrEmpty(user.Password))
                        {
                            var (salt, passwordHash) = PasswordHasher.HashPassword(user.Password);
                            
                            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                            command.Parameters.AddWithValue("@Salt", salt);
                            command.Parameters.AddWithValue("@UpdatePassword", true);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@PasswordHash", DBNull.Value);
                            command.Parameters.AddWithValue("@Salt", DBNull.Value);
                            command.Parameters.AddWithValue("@UpdatePassword", false);
                        }
                        
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error updating user: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a user
        /// </summary>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_DeleteUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error deleting user: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Resets a user's password
        /// </summary>
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword, int updatedBy)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var (salt, passwordHash) = PasswordHasher.HashPassword(newPassword);
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_ResetUserPassword", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        command.Parameters.AddWithValue("@Salt", salt);
                        command.Parameters.AddWithValue("@MustChangePassword", true);
                        command.Parameters.AddWithValue("@UpdatedBy", updatedBy == 0 ? (object)DBNull.Value : updatedBy);
                        
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error resetting password: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Locks or unlocks a user
        /// </summary>
        public async Task<bool> LockUnlockUserAsync(int userId, bool lockStatus, int updatedBy)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_LockUnlockUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@IsLockedOut", lockStatus);
                        command.Parameters.AddWithValue("@UpdatedBy", updatedBy == 0 ? (object)DBNull.Value : updatedBy);
                        
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error locking/unlocking user: {ex.Message}");
                throw;
            }
        }
    }
}
