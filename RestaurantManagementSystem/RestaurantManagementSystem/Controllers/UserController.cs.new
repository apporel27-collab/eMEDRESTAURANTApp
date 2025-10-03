using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RestaurantManagementSystem.Controllers
{
    public class UserController : Controller
    {
        private readonly IConfiguration _config;

        public UserController(IConfiguration configuration)
        {
            _config = configuration;
        }

        // Users List
        public IActionResult UserList()
        {
            try
            {
                var users = new List<User>();
                using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    
                    // First ensure Users table exists
                    EnsureUsersTableExists(con);
                    
                    // Fix User table schema if needed
                    EnsureUserTableColumns(con);
                    
                    // Use a simpler and safer query approach
                    using (var cmd = new SqlCommand("SELECT Id, Username, FirstName, LastName, Email, IsActive FROM Users", con))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                    FirstName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    LastName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                    Phone = string.Empty, // Default value
                                    Role = UserRole.Guest, // Default value
                                    IsActive = reader.GetBoolean(5)
                                });
                            }
                        }
                    }
                }
                
                return View(users);
            }
            catch (Exception ex)
            {
                // Display error in a friendly way
                ViewBag.ErrorMessage = $"Error loading users: {ex.Message}";
                return View(new List<User>());
            }
        }

        // User Add/Edit/View Form
        public IActionResult UserForm(int? id, bool isView = false)
        {
            try
            {
                User model = new User { Username = "", FirstName = "", LastName = "" };
                ViewBag.IsView = isView;
                ViewBag.Roles = Enum.GetValues(typeof(UserRole))
                    .Cast<UserRole>()
                    .Where(r => r < UserRole.CRMMarketing) // Filter out system integration roles
                    .Select(r => new { Id = (int)r, Name = r.ToString() })
                    .ToList();

                if (id.HasValue)
                {
                    using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        con.Open();
                        
                        // Ensure Users table and columns exist
                        EnsureUsersTableExists(con);
                        EnsureUserTableColumns(con);
                        
                        using (var cmd = new SqlCommand("SELECT Id, Username, FirstName, LastName, Email, IsActive FROM Users WHERE Id = @Id", con))
                        {
                            cmd.Parameters.AddWithValue("@Id", id.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    model = new User
                                    {
                                        Id = reader.GetInt32(0),
                                        Username = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                        FirstName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                        LastName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                        Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                        Phone = string.Empty, // Default value
                                        Role = UserRole.Guest, // Default value
                                        IsActive = reader.GetBoolean(5)
                                    };
                                }
                            }
                        }
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                // Display error in a friendly way
                ViewBag.ErrorMessage = $"Error loading user: {ex.Message}";
                return View(new User { Username = "", FirstName = "", LastName = "" });
            }
        }

        // Save User
        [HttpPost]
        public IActionResult SaveUser(User model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string resultMessage = "";
                    bool isUsernameInUse = UserExists(model.Username, model.Id > 0 ? model.Id : null);

                    if (isUsernameInUse)
                    {
                        ModelState.AddModelError("Username", "Username is already in use");
                        ViewBag.Roles = Enum.GetValues(typeof(UserRole))
                            .Cast<UserRole>()
                            .Where(r => r < UserRole.CRMMarketing)
                            .Select(r => new { Id = (int)r, Name = r.ToString() })
                            .ToList();
                        return View("UserForm", model);
                    }

                    using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        con.Open();
                        
                        // Ensure Users table and columns exist
                        EnsureUsersTableExists(con);
                        EnsureUserTableColumns(con);
                        
                        string sql;
                        if (model.Id > 0)
                        {
                            // Update
                            sql = @"
                                UPDATE Users SET 
                                    Username = @Username, 
                                    FirstName = @FirstName, 
                                    LastName = @LastName,
                                    Email = @Email, 
                                    IsActive = @IsActive
                                WHERE Id = @Id;
                                SELECT 'User updated successfully' as Message;";
                        }
                        else
                        {
                            // Insert
                            sql = @"
                                INSERT INTO Users (Username, Password, FirstName, LastName, Email, IsActive)
                                VALUES (@Username, @Password, @FirstName, @LastName, @Email, @IsActive);
                                SELECT 'User added successfully' as Message;";
                        }

                        using (var cmd = new SqlCommand(sql, con))
                        {
                            if (model.Id > 0)
                            {
                                cmd.Parameters.AddWithValue("@Id", model.Id);
                            }
                            
                            cmd.Parameters.AddWithValue("@Username", model.Username);

                            if (model.Id > 0)
                            {
                                // No password update on edit
                            }
                            else
                            {
                                // For new users, set a default password
                                cmd.Parameters.AddWithValue("@Password", "password123"); // In production, use proper password hashing
                            }
                            
                            cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                            cmd.Parameters.AddWithValue("@LastName", model.LastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    resultMessage = reader["Message"].ToString();
                                }
                            }
                        }
                    }
                    TempData["ResultMessage"] = resultMessage;
                    return RedirectToAction("UserList");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error saving user: {ex.Message}");
                }
            }
            
            ViewBag.Roles = Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .Where(r => r < UserRole.CRMMarketing)
                .Select(r => new { Id = (int)r, Name = r.ToString() })
                .ToList();
            return View("UserForm", model);
        }

        private bool UserExists(string username, int? excludeId = null)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    
                    // Ensure Users table exists
                    EnsureUsersTableExists(con);
                    
                    string sql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
                    if (excludeId.HasValue)
                    {
                        sql += " AND Id <> @Id";
                    }
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        if (excludeId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@Id", excludeId.Value);
                        }
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false; // Assume username doesn't exist if there's an error
            }
        }
        
        private void EnsureUsersTableExists(SqlConnection connection)
        {
            try
            {
                // Check if Users table exists
                bool tableExists = false;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM sys.tables WHERE name = 'Users'", connection))
                {
                    tableExists = ((int)cmd.ExecuteScalar() > 0);
                }
                
                // Create Users table if it doesn't exist
                if (!tableExists)
                {
                    using (var cmd = new SqlCommand(@"
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Username NVARCHAR(50) NOT NULL UNIQUE,
                            Password NVARCHAR(255) NOT NULL,
                            FirstName NVARCHAR(50) NULL,
                            LastName NVARCHAR(50) NULL,
                            Email NVARCHAR(100) NULL,
                            Phone NVARCHAR(20) NULL,
                            Role INT NOT NULL DEFAULT 3,
                            IsActive BIT NOT NULL DEFAULT 1,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            LastLogin DATETIME NULL
                        )", connection))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Created Users table");
                        
                        // Add admin user
                        using (var insertCmd = new SqlCommand(@"
                            INSERT INTO Users (Username, Password, FirstName, LastName, Email, Role, IsActive)
                            VALUES ('admin', 'password123', 'System', 'Administrator', 'admin@restaurant.com', 12, 1)",
                            connection))
                        {
                            insertCmd.ExecuteNonQuery();
                            Console.WriteLine("Created admin user");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring Users table exists: {ex.Message}");
            }
        }
        
        private void EnsureUserTableColumns(SqlConnection connection)
        {
            try
            {
                // Check and add Phone column if needed
                bool phoneExists = false;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Phone'", connection))
                {
                    phoneExists = ((int)cmd.ExecuteScalar() > 0);
                }
                
                if (!phoneExists)
                {
                    using (var cmd = new SqlCommand(
                        "ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(20) NULL", connection))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Added Phone column to Users table");
                    }
                }

                // Check and add Role column if needed
                bool roleExists = false;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Role'", connection))
                {
                    roleExists = ((int)cmd.ExecuteScalar() > 0);
                }
                
                if (!roleExists)
                {
                    using (var cmd = new SqlCommand(
                        "ALTER TABLE [dbo].[Users] ADD [Role] INT NOT NULL DEFAULT 3", connection))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Added Role column to Users table");
                    }
                }
                
                // Check if RoleId column exists
                bool roleIdExists = false;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'RoleId'", connection))
                {
                    roleIdExists = ((int)cmd.ExecuteScalar() > 0);
                }
                
                // Only try to update if both columns exist
                if (roleExists && roleIdExists)
                {
                    using (var cmd = new SqlCommand(
                        "UPDATE [dbo].[Users] SET [Role] = [RoleId]", connection))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Updated Role column with values from RoleId");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring User table columns: {ex.Message}");
            }
        }
    }
}
