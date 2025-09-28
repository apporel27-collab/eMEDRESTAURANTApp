using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;
using System.Linq;

namespace RestaurantManagementSystem.Services
{
    public class UserRoleService
    {
        private readonly string _connectionString;
        
        public UserRoleService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        /// <summary>
        /// Gets all roles from the database
        /// </summary>
        public async Task<List<Role>> GetAllRolesAsync()
        {
            var roles = new List<Role>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Use direct SQL query instead of stored procedure
                    string sql = @"SELECT Id, Name, Description, 
                                  CASE WHEN IsSystemRole IS NULL THEN 0 ELSE IsSystemRole END AS IsSystemRole 
                                  FROM purojit2_idmcbp.Roles";
                                  
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                roles.Add(new Role
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"]?.ToString(),
                                    IsSystemRole = Convert.ToBoolean(reader["IsSystemRole"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting roles: {ex.Message}");
                throw;
            }
            
            return roles;
        }
        
        /// <summary>
        /// Gets a role by its ID, including users assigned to the role
        /// </summary>
        // Original method with original return type
        public async Task<(Role Role, List<User> Users)> GetRoleWithUsersAsync(int id)
        {
            Role role = null;
            var users = new List<User>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_GetRoleById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", id);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set: Role details
                            if (await reader.ReadAsync())
                            {
                                role = new Role
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"]?.ToString(),
                                    IsSystemRole = Convert.ToBoolean(reader["IsSystemRole"])
                                };
                            }
                            
                            // Second result set: Users in this role
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    users.Add(new User
                                    {
                                        Id = Convert.ToInt32(reader["Id"]),
                                        Username = reader["Username"].ToString(),
                                        FirstName = reader["FirstName"]?.ToString(),
                                        LastName = reader["LastName"]?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting role by ID {id}: {ex.Message}");
                throw;
            }
            
            return (role, users);
        }
        
        /// <summary>
        /// Gets a role by its ID with success/failure information
        /// </summary>
        public async Task<(bool Success, string Message, Role role)> GetRoleByIdAsync(int id)
        {
            try
            {
                var result = await GetRoleWithUsersAsync(id);
                if (result.Role == null)
                {
                    return (false, $"Role with ID {id} not found", null);
                }
                return (true, "Role retrieved successfully", result.Role);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting role by ID {id}: {ex.Message}");
                return (false, $"Error retrieving role: {ex.Message}", null);
            }
        }
        
        /// <summary>
        /// Creates a new role
        /// </summary>
        public async Task<(bool Success, string Message)> CreateRoleAsync(Role role)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_CreateRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@Name", role.Name);
                        command.Parameters.AddWithValue("@Description", 
                            string.IsNullOrEmpty(role.Description) ? DBNull.Value : (object)role.Description);
                        command.Parameters.AddWithValue("@IsSystemRole", role.IsSystemRole);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int roleId = Convert.ToInt32(reader["RoleId"]);
                                string message = reader["Message"].ToString();
                                
                                return (true, message);
                            }
                        }
                    }
                }
                
                return (false, "Failed to create role");
            }
            catch (SqlException ex)
            {
                // Handle specific SQL exceptions
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error creating role: {ex.Message}");
                return (false, "An error occurred while creating the role");
            }
        }
        
        /// <summary>
        /// Updates an existing role
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateRoleAsync(Role role)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_UpdateRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@Id", role.Id);
                        command.Parameters.AddWithValue("@Name", role.Name);
                        command.Parameters.AddWithValue("@Description", 
                            string.IsNullOrEmpty(role.Description) ? DBNull.Value : (object)role.Description);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string message = reader["Message"].ToString();
                                return (true, message);
                            }
                        }
                    }
                }
                
                return (false, "Failed to update role");
            }
            catch (SqlException ex)
            {
                // Handle specific SQL exceptions
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error updating role: {ex.Message}");
                return (false, "An error occurred while updating the role");
            }
        }
        
        /// <summary>
        /// Deletes a role
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteRoleAsync(int id)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_DeleteRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", id);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string message = reader["Message"].ToString();
                                return (true, message);
                            }
                        }
                    }
                }
                
                return (false, "Failed to delete role");
            }
            catch (SqlException ex)
            {
                // Handle specific SQL exceptions
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error deleting role: {ex.Message}");
                return (false, "An error occurred while deleting the role");
            }
        }
        
        /// <summary>
        /// Gets roles for a specific user
        /// </summary>
        public async Task<List<Role>> GetUserRolesAsync(int userId)
        {
            var roles = new List<Role>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Use direct SQL query instead of stored procedure
                    string sql = @"
                        SELECT r.Id AS RoleId, r.Name AS RoleName, r.Description AS RoleDescription
                        FROM purojit2_idmcbp.Roles r
                        INNER JOIN purojit2_idmcbp.UserRoles ur ON r.Id = ur.RoleId
                        WHERE ur.UserId = @UserId";
                        
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                roles.Add(new Role
                                {
                                    Id = Convert.ToInt32(reader["RoleId"]),
                                    Name = reader["RoleName"].ToString(),
                                    Description = reader["RoleDescription"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting roles for user {userId}: {ex.Message}");
                throw;
            }
            
            return roles;
        }
        
        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        public async Task<(bool Success, string Message)> AssignRoleToUserAsync(int userId, int roleId)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"INSERT INTO purojit2_idmcbp.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId); SELECT 'Role assigned' AS Message;", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@RoleId", roleId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string message = reader["Message"].ToString();
                                return (true, message);
                            }
                        }
                    }
                }
                
                return (false, "Failed to assign role to user");
            }
            catch (SqlException ex)
            {
                // Handle specific SQL exceptions
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error assigning role to user: {ex.Message}");
                return (false, "An error occurred while assigning the role");
            }
        }
        
        /// <summary>
        /// Removes a role from a user
        /// </summary>
        public async Task<(bool Success, string Message)> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"DELETE FROM purojit2_idmcbp.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId; SELECT 'Role removed' AS Message;", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@RoleId", roleId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string message = reader["Message"].ToString();
                                return (true, message);
                            }
                        }
                    }
                }
                
                return (false, "Failed to remove role from user");
            }
            catch (SqlException ex)
            {
                // Handle specific SQL exceptions
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error removing role from user: {ex.Message}");
                return (false, "An error occurred while removing the role");
            }
        }

        /// <summary>
        /// Gets all roles for a specific user by name
        /// </summary>
        public async Task<IEnumerable<string>> GetRolesForUserAsync(int userId)
        {
            var roles = await GetUserRolesAsync(userId);
            return roles.Select(r => r.Name);
        }

        /// <summary>
        /// Sets multiple roles for a user at once
        /// </summary>
        public async Task<(bool Success, string Message)> SetUserRolesAsync(int userId, List<int> roleIds)
        {
            try
            {
                // First remove all existing roles for this user
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"DELETE FROM purojit2_idmcbp.UserRoles WHERE UserId = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // Then add the new roles
                    bool success = true;
                    string message = "Roles updated successfully";
                    
                    foreach (var roleId in roleIds)
                    {
                        var result = await AssignRoleToUserAsync(userId, roleId);
                        if (!result.Success)
                        {
                            success = false;
                            message = result.Message;
                            break;
                        }
                    }
                    
                    return (success, message);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error setting user roles: {ex.Message}");
                return (false, $"An error occurred while setting user roles: {ex.Message}");
            }
        }
        
        // Add alias method for backward compatibility
        public async Task<(bool Success, string Message)> UpdateUserRolesAsync(int userId, List<int> roleIds)
        {
            return await SetUserRolesAsync(userId, roleIds);
        }

        /// <summary>
        /// Gets all users with their role status for a specific role
        /// </summary>
        public async Task<IEnumerable<UserRoleViewModel>> GetAllUsersWithRoleStatusAsync(int roleId)
        {
            var userRoles = new List<UserRoleViewModel>();
            
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_GetAllUsersWithRoleStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@RoleId", roleId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                userRoles.Add(new UserRoleViewModel
                                {
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    Username = reader["Username"].ToString(),
                                    FullName = $"{reader["FirstName"]} {reader["LastName"]}".Trim(),
                                    Email = reader["Email"]?.ToString(),
                                    HasRole = Convert.ToBoolean(reader["HasRole"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting users with role status: {ex.Message}");
            }
            
            return userRoles;
        }

        /// <summary>
        /// Sets multiple users for a specific role at once
        /// </summary>
        public async Task<(bool Success, string Message)> SetUsersForRoleAsync(int roleId, List<int> userIds)
        {
            try
            {
                // First remove all users from this role
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_RemoveAllUsersFromRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@RoleId", roleId);
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // Then add the new users
                    bool success = true;
                    string message = "Role assignments updated successfully";
                    
                    foreach (var userId in userIds)
                    {
                        var result = await AssignRoleToUserAsync(userId, roleId);
                        if (!result.Success)
                        {
                            success = false;
                            message = result.Message;
                            break;
                        }
                    }
                    
                    return (success, message);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error setting role users: {ex.Message}");
                return (false, $"An error occurred while setting role users: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// View Model for User Role Management
    /// </summary>
    // This is an internal ViewModel used by the service
    public class UserRoleViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool HasRole { get; set; }
        
        // Property for compatibility with ViewModels.UserRoleViewModel
        public bool IsAssigned 
        { 
            get { return HasRole; }
            set { HasRole = value; }
        }
    }
}
