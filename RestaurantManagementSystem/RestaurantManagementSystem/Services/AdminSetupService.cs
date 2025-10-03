using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace RestaurantManagementSystem.Services
{
    public class AdminSetupService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminSetupService> _logger;

        public AdminSetupService(IConfiguration configuration, ILogger<AdminSetupService> logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnsureAdminUserAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    _logger?.LogInformation("Connected to the database to check for admin user");

                    // Check if admin user exists
                    bool adminExists = false;
                    using (var command = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Username = 'admin'", connection))
                    {
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        adminExists = count > 0;
                    }

                    if (!adminExists)
                    {
                        _logger?.LogInformation("Admin user does not exist. Creating default admin user.");
                        
                        // Create the admin user with known credentials
                        string passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                        
                        using (var command = new SqlCommand(
                            "INSERT INTO Users (Username, PasswordHash, FirstName, LastName, Email, IsActive, RequiresMFA, CreatedAt, UpdatedAt) " +
                            "VALUES ('admin', @PasswordHash, 'Admin', 'User', 'admin@example.com', 1, 0, @CreatedAt, @UpdatedAt)", connection))
                        {
                            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            await command.ExecuteNonQueryAsync();
                            _logger?.LogInformation("Default admin user created successfully");
                        }

                        // Get the user ID to assign admin role
                        int adminUserId;
                        using (var command = new SqlCommand("SELECT Id FROM Users WHERE Username = 'admin'", connection))
                        {
                            adminUserId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        // Check if we have an admin role, create if not
                        int adminRoleId;
                        using (var command = new SqlCommand("SELECT COUNT(1) FROM Roles WHERE Name = 'Administrator'", connection))
                        {
                            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                            if (count == 0)
                            {
                                _logger?.LogInformation("Admin role does not exist. Creating admin role.");
                                using (var createRoleCommand = new SqlCommand(
                                    "INSERT INTO Roles (Name, Description) VALUES ('Administrator', 'System Administrator'); SELECT SCOPE_IDENTITY();", 
                                    connection))
                                {
                                    adminRoleId = Convert.ToInt32(await createRoleCommand.ExecuteScalarAsync());
                                }
                            }
                            else
                            {
                                using (var getRoleCommand = new SqlCommand("SELECT Id FROM Roles WHERE Name = 'Administrator'", connection))
                                {
                                    adminRoleId = Convert.ToInt32(await getRoleCommand.ExecuteScalarAsync());
                                }
                            }
                        }

                        // Assign admin role to admin user
                        using (var command = new SqlCommand(
                            "INSERT INTO UserRoles (UserId, RoleId, AssignedAt) VALUES (@UserId, @RoleId, @AssignedAt)", connection))
                        {
                            command.Parameters.AddWithValue("@UserId", adminUserId);
                            command.Parameters.AddWithValue("@RoleId", adminRoleId);
                            command.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                            await command.ExecuteNonQueryAsync();
                            _logger?.LogInformation("Admin role assigned to admin user");
                        }
                    }
                    else
                    {
                        _logger?.LogInformation("Admin user already exists");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ensuring admin user exists");
            }
        }
    }
}
