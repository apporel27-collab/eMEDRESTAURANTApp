using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagementSystem.Controllers
{
    public class UserController : Controller
    {
        // Stub: Ensures Users table exists (implement as needed)
        private void EnsureUsersTableExists(SqlConnection con) { /* TODO: Implement schema check if needed */ }

        // Stub: Ensures required columns exist in Users table (implement as needed)
        private void EnsureUserTableColumns(SqlConnection con) { /* TODO: Implement column check if needed */ }

        // Stub: Checks if a username exists (implement as needed)
        private bool UserExists(string username, int? excludeUserId = null) { return false; /* TODO: Implement actual check */ }
        private readonly IConfiguration _config;
        private readonly UserRoleService _userRoleService;

        public UserController(IConfiguration configuration, UserRoleService userRoleService)
        {
            _config = configuration;
            _userRoleService = userRoleService;
        }

        // Users List
        public async Task<IActionResult> UserList()
        {
            try
            {
                var users = new List<User>();
                using (var con = new Microsoft.Data.SqlClient.SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    
                    // First ensure Users table exists
                    EnsureUsersTableExists(con);
                    
                    // Create or update a robust stored procedure to list users safely across schema variants
                    var createSp = @"CREATE OR ALTER PROCEDURE dbo.usp_GetUsersList
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @hasPhone bit = CASE WHEN COL_LENGTH('purojit2_idmcbp.Users','Phone') IS NOT NULL THEN 1 ELSE 0 END;
    
    DECLARE @sql nvarchar(max) = N'SELECT Id, Username, FirstName, LastName, Email, IsActive, ' +
        CASE WHEN @hasPhone=1 THEN N'Phone' ELSE N'CAST(NULL AS NVARCHAR(20)) AS Phone' END +
        N' FROM purojit2_idmcbp.Users';
    EXEC sp_executesql @sql;
END";
                    using (var createCmd = new Microsoft.Data.SqlClient.SqlCommand(createSp, con))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_GetUsersList", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                    FirstName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    LastName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                    IsActive = reader.GetBoolean(5),
                                    Phone = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty
                                };
                                users.Add(user);
                            }
                        }
                    }
                }
                
                // For each user, get their roles
                foreach (var user in users)
                {
                    user.Roles = (await _userRoleService.GetUserRolesAsync(user.Id)).ToList();
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
        public async Task<IActionResult> UserForm(int? id, bool isView = false)
        {
            try
            {
                User model = new User { Username = "", FirstName = "", LastName = "" };
                ViewBag.IsView = isView;
                
                // Get all roles for dropdown
                var allRoles = await _userRoleService.GetAllRolesAsync();
                ViewBag.AllRoles = allRoles;
                ViewBag.Roles = allRoles; // Adding this for backward compatibility

                if (id.HasValue)
                {
                    using (var con = new Microsoft.Data.SqlClient.SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        con.Open();
                        
                        // Ensure Users table and columns exist
                        EnsureUsersTableExists(con);
                        EnsureUserTableColumns(con);
                        
                        // Create or alter a stored procedure to fetch a single user robustly
                        var createSp = @"CREATE OR ALTER PROCEDURE dbo.usp_GetUserById
                            @Id INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            DECLARE @hasPhone bit = CASE WHEN COL_LENGTH('purojit2_idmcbp.Users','Phone') IS NOT NULL THEN 1 ELSE 0 END;
                            
                            DECLARE @sql nvarchar(max) = N'SELECT Id, Username, FirstName, LastName, Email, IsActive, ' +
                                CASE WHEN @hasPhone=1 THEN N'Phone' ELSE N'CAST(NULL AS NVARCHAR(20)) AS Phone' END +
                                N' FROM purojit2_idmcbp.Users WHERE Id = @Id';
                            EXEC sp_executesql @sql, N'@Id int', @Id=@Id;
                        END";
                        using (var createCmd = new Microsoft.Data.SqlClient.SqlCommand(createSp, con))
                        {
                            createCmd.ExecuteNonQuery();
                        }
                        using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("dbo.usp_GetUserById", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
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
                                        IsActive = reader.GetBoolean(5),
                                        Phone = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty
                                    };
                                }
                            }
                        }
                    }
                    
                    // Get user roles
                    if (model.Id > 0)
                    {
                        model.Roles = (await _userRoleService.GetUserRolesAsync(model.Id)).ToList();
                        
                        // Populate the SelectedRoleIds based on assigned roles
                        model.SelectedRoleIds = model.Roles.Select(r => r.Id).ToList();
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
        [HttpPostAttribute]
        public async Task<IActionResult> SaveUser(User model, List<int> selectedRoles)
        {
            // Always remove Password validation for existing users
            if (model.Id > 0 && ModelState.ContainsKey("Password"))
            {
                ModelState.Remove("Password");
            }
            
            // Handle password validation/binding for create vs edit
            if (model.Id == 0)
            {
                var postedPassword = Request.Form["password"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(postedPassword))
                {
                    ModelState.AddModelError("Password", "Password is required for new users.");
                }
                else
                {
                    model.Password = postedPassword.Trim();
                    // Remove default model state error for Password since we're setting it manually
                    if (ModelState.ContainsKey("Password")) ModelState.Remove("Password");
                }
            }
            else
            {
                var postedPassword = Request.Form["password"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(postedPassword))
                {
                    model.Password = postedPassword.Trim();
                }
            }
            
            // Boolean properties in C# are already non-nullable value types
            // The bool type can't be null, so no need to check for null
            
            // Get all roles for dropdown in case we need to return the view
            var allRoles = await _userRoleService.GetAllRolesAsync();
            ViewBag.AllRoles = allRoles;
            ViewBag.Roles = allRoles; // Adding this for backward compatibility

            if (ModelState.IsValid)
            {
                try
                {
                    string resultMessage = "";
                    bool isUsernameInUse = UserExists(model.Username, model.Id > 0 ? model.Id : null);

                    if (isUsernameInUse)
                    {
                        ModelState.AddModelError("Username", "Username is already in use");
                        return View("UserForm", model);
                    // End of isUsernameInUse block
                }

                    using (var con = new Microsoft.Data.SqlClient.SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        con.Open();
                        
                        // Ensure Users table and columns exist
                        EnsureUsersTableExists(con);
                        EnsureUserTableColumns(con);
                        
                        int userId = model.Id;
                        string roleIds = selectedRoles != null && selectedRoles.Count > 0 ? string.Join(",", selectedRoles) : "";
                        // Determine password and salt to use. If a plaintext password was provided, hash it with BCrypt and save its salt.
                        string passwordToUse = null;
                        string saltToUse = null;
                        if (!string.IsNullOrWhiteSpace(model.Password))
                        {
                            // If the provided value already looks like a BCrypt hash, use as-is and extract salt
                            var p = model.Password.Trim();
                            if (p.StartsWith("$2a$") || p.StartsWith("$2b$") || p.StartsWith("$2y$"))
                            {
                                passwordToUse = p;
                                // bcrypt salt is the first 29 chars of the hash
                                if (p.Length >= 29) saltToUse = p.Substring(0, 29);
                            }
                            else
                            {
                                saltToUse = BCrypt.Net.BCrypt.GenerateSalt(12);
                                passwordToUse = BCrypt.Net.BCrypt.HashPassword(p, saltToUse);
                            }
                        }
                        else if (model.Id > 0)
                        {
                            // No new password supplied for edit — preserve current stored hash and salt
                            using (var pwdCmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT PasswordHash, Salt FROM purojit2_idmcbp.Users WHERE Id = @Id", con))
                            {
                                pwdCmd.Parameters.AddWithValue("@Id", model.Id);
                                using (var rdr = pwdCmd.ExecuteReader())
                                {
                                    if (rdr.Read())
                                    {
                                        passwordToUse = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                                        saltToUse = rdr.FieldCount > 1 && !rdr.IsDBNull(1) ? rdr.GetString(1) : null;
                                    }
                                }
                            }
                        }
                        using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("purojit2_idmcbp.usp_CreateOrUpdateUserWithRoles", con))
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Id", model.Id);
                            cmd.Parameters.AddWithValue("@Username", model.Username);
                            cmd.Parameters.AddWithValue("@PasswordHash", (object)passwordToUse ?? (object)DBNull.Value);
                            // Ensure we never pass NULL for Salt; use extracted/generated salt or empty string fallback
                            cmd.Parameters.AddWithValue("@Salt", (object)(saltToUse ?? string.Empty));
                            cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                            cmd.Parameters.AddWithValue("@LastName", model.LastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(model.Phone) ? (object)DBNull.Value : model.Phone);
                            cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                            cmd.Parameters.AddWithValue("@RoleIds", roleIds);
                            var result = cmd.ExecuteReader();
                            if (result.Read())
                            {
                                userId = Convert.ToInt32(result["UserId"]);
                            }
                            result.Close();
                            resultMessage = model.Id == 0 ? "User added successfully" : "User updated successfully";
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
            // Get roles for dropdown before returning
            var userRoles = _userRoleService.GetAllRolesAsync().Result;
            ViewBag.AllRoles = userRoles;
            ViewBag.Roles = userRoles; // Adding this for backward compatibility
            // Show all ModelState errors in TempData for debugging
            if (!ModelState.IsValid)
            {
                var allErrors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["DebugErrors"] = allErrors;
            }
            return View("UserForm", model);
        }
        }
    }
