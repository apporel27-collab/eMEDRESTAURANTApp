-- User Authentication and Management stored procedures

-- Get all users with their roles
CREATE OR ALTER PROCEDURE dbo.usp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT u.Id, u.Username, u.FirstName, u.LastName, u.Email, u.Phone, u.IsActive,
           u.IsLockedOut, u.FailedLoginAttempts, u.LastLoginDate, u.CreatedAt, u.UpdatedAt,
           STRING_AGG(r.Name, ', ') AS Roles
    FROM dbo.Users u
    LEFT JOIN dbo.UserRoles ur ON u.Id = ur.UserId
    LEFT JOIN dbo.Roles r ON ur.RoleId = r.Id
    GROUP BY u.Id, u.Username, u.FirstName, u.LastName, u.Email, u.Phone, u.IsActive,
             u.IsLockedOut, u.FailedLoginAttempts, u.LastLoginDate, u.CreatedAt, u.UpdatedAt;
END;
GO

-- Get user by ID with roles
CREATE OR ALTER PROCEDURE dbo.usp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- User details
    SELECT u.Id, u.Username, u.FirstName, u.LastName, u.Email, u.Phone, u.IsActive,
           u.IsLockedOut, u.FailedLoginAttempts, u.LastLoginDate, u.CreatedAt, u.UpdatedAt
    FROM dbo.Users u
    WHERE u.Id = @UserId;
    
    -- User roles
    SELECT r.Id, r.Name, r.Description
    FROM dbo.Roles r
    JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
    WHERE ur.UserId = @UserId;
    
    -- All available roles for selection
    SELECT r.Id, r.Name, r.Description,
           CASE WHEN ur.UserId IS NOT NULL THEN 1 ELSE 0 END AS IsAssigned
    FROM dbo.Roles r
    LEFT JOIN dbo.UserRoles ur ON r.Id = ur.RoleId AND ur.UserId = @UserId;
END;
GO

-- Get user by username (used for authentication)
CREATE OR ALTER PROCEDURE dbo.usp_GetUserByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- User details including password hash and salt for authentication
    SELECT u.Id, u.Username, u.FirstName, u.LastName, u.Email, u.Phone, u.IsActive,
           u.PasswordHash, u.Salt, u.IsLockedOut, u.FailedLoginAttempts, 
           u.RequiresMFA, u.MustChangePassword,
           u.LastLoginDate, u.CreatedAt, u.UpdatedAt
    FROM dbo.Users u
    WHERE u.Username = @Username;
    
    -- User roles
    SELECT r.Id, r.Name, r.Description
    FROM dbo.Roles r
    JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
    JOIN dbo.Users u ON ur.UserId = u.Id
    WHERE u.Username = @Username;
END;
GO

-- Create new user
CREATE OR ALTER PROCEDURE dbo.usp_CreateUser
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255),
    @Salt NVARCHAR(100),
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT = 1,
    @IsLockedOut BIT = 0,
    @RequiresMFA BIT = 0,
    @MustChangePassword BIT = 0,
    @CreatedBy INT = NULL,
    @UserId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Create the user
        INSERT INTO dbo.Users (
            Username, PasswordHash, Salt, FirstName, LastName,
            Email, Phone, IsActive, IsLockedOut,
            FailedLoginAttempts, RequiresMFA, MustChangePassword,
            CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
        )
        VALUES (
            @Username, @PasswordHash, @Salt, @FirstName, @LastName,
            @Email, @Phone, @IsActive, @IsLockedOut,
            0, @RequiresMFA, @MustChangePassword,
            GETDATE(), GETDATE(), @CreatedBy, @CreatedBy
        );
        
        SET @UserId = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Rethrow the error
        THROW;
    END CATCH;
END;
GO

-- Update existing user
CREATE OR ALTER PROCEDURE dbo.usp_UpdateUser
    @UserId INT,
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255) = NULL,
    @Salt NVARCHAR(100) = NULL,
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT,
    @IsLockedOut BIT,
    @RequiresMFA BIT = 0,
    @MustChangePassword BIT = 0,
    @UpdatedBy INT = NULL,
    @UpdatePassword BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Update the user
        IF @UpdatePassword = 1
        BEGIN
            UPDATE dbo.Users
            SET Username = @Username,
                PasswordHash = @PasswordHash,
                Salt = @Salt,
                FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                Phone = @Phone,
                IsActive = @IsActive,
                IsLockedOut = @IsLockedOut,
                RequiresMFA = @RequiresMFA,
                MustChangePassword = @MustChangePassword,
                UpdatedAt = GETDATE(),
                UpdatedBy = @UpdatedBy
            WHERE Id = @UserId;
        END
        ELSE
        BEGIN
            UPDATE dbo.Users
            SET Username = @Username,
                FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                Phone = @Phone,
                IsActive = @IsActive,
                IsLockedOut = @IsLockedOut,
                RequiresMFA = @RequiresMFA,
                MustChangePassword = @MustChangePassword,
                UpdatedAt = GETDATE(),
                UpdatedBy = @UpdatedBy
            WHERE Id = @UserId;
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Rethrow the error
        THROW;
    END CATCH;
END;
GO

-- Delete user
CREATE OR ALTER PROCEDURE dbo.usp_DeleteUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Delete user's roles first
        DELETE FROM dbo.UserRoles
        WHERE UserId = @UserId;
        
        -- Delete the user
        DELETE FROM dbo.Users
        WHERE Id = @UserId;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Rethrow the error
        THROW;
    END CATCH;
END;
GO

-- Record login attempt and handle account lockout
CREATE OR ALTER PROCEDURE dbo.usp_RecordLoginAttempt
    @UserId INT,
    @Successful BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF @Successful = 1
        BEGIN
            -- Successful login
            UPDATE dbo.Users
            SET LastLoginDate = GETDATE(),
                FailedLoginAttempts = 0
            WHERE Id = @UserId;
        END
        ELSE
        BEGIN
            -- Failed login, increment attempts and check for lockout
            UPDATE dbo.Users
            SET FailedLoginAttempts = FailedLoginAttempts + 1,
                IsLockedOut = CASE 
                                WHEN FailedLoginAttempts + 1 >= 5 THEN 1 
                                ELSE IsLockedOut 
                              END
            WHERE Id = @UserId;
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Rethrow the error
        THROW;
    END CATCH;
END;
GO

-- Reset user password
CREATE OR ALTER PROCEDURE dbo.usp_ResetUserPassword
    @UserId INT,
    @PasswordHash NVARCHAR(255),
    @Salt NVARCHAR(100),
    @MustChangePassword BIT = 1,
    @UpdatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Users
    SET PasswordHash = @PasswordHash,
        Salt = @Salt,
        MustChangePassword = @MustChangePassword,
        UpdatedAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @UserId;
END;
GO

-- Lock or unlock a user account
CREATE OR ALTER PROCEDURE dbo.usp_LockUnlockUser
    @UserId INT,
    @IsLockedOut BIT,
    @UpdatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Users
    SET IsLockedOut = @IsLockedOut,
        -- If unlocking, reset failed login attempts
        FailedLoginAttempts = CASE WHEN @IsLockedOut = 0 THEN 0 ELSE FailedLoginAttempts END,
        UpdatedAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @UserId;
END;
GO

-- Get all roles
CREATE OR ALTER PROCEDURE dbo.usp_GetAllRoles
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT r.Id, r.Name, r.Description, r.IsSystemRole, r.CreatedAt, r.UpdatedAt
    FROM dbo.Roles r
    ORDER BY r.Name;
END;
GO

-- Get role by ID with assigned users
CREATE OR ALTER PROCEDURE dbo.usp_GetRoleById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Role details
    SELECT r.Id, r.Name, r.Description, r.IsSystemRole, r.CreatedAt, r.UpdatedAt
    FROM dbo.Roles r
    WHERE r.Id = @Id;
    
    -- Users assigned to this role
    SELECT u.Id, u.Username, u.FirstName, u.LastName, u.Email, u.Phone, u.IsActive
    FROM dbo.Users u
    JOIN dbo.UserRoles ur ON u.Id = ur.UserId
    WHERE ur.RoleId = @Id
    ORDER BY u.Username;
    
    -- All users for selection
    SELECT u.Id, u.Username, u.FirstName, u.LastName, u.Email,
           CASE WHEN ur.RoleId IS NOT NULL THEN 1 ELSE 0 END AS IsAssigned
    FROM dbo.Users u
    LEFT JOIN dbo.UserRoles ur ON u.Id = ur.UserId AND ur.RoleId = @Id
    WHERE u.IsActive = 1
    ORDER BY u.Username;
END;
GO

-- Create new role
CREATE OR ALTER PROCEDURE dbo.usp_CreateRole
    @Name NVARCHAR(50),
    @Description NVARCHAR(255) = NULL,
    @IsSystemRole BIT = 0,
    @RoleId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.Roles (Name, Description, IsSystemRole, CreatedAt, UpdatedAt)
    VALUES (@Name, @Description, @IsSystemRole, GETDATE(), GETDATE());
    
    SET @RoleId = SCOPE_IDENTITY();
END;
GO

-- Update existing role
CREATE OR ALTER PROCEDURE dbo.usp_UpdateRole
    @Id INT,
    @Name NVARCHAR(50),
    @Description NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Roles
    SET Name = @Name,
        Description = @Description,
        UpdatedAt = GETDATE()
    WHERE Id = @Id AND IsSystemRole = 0; -- Don't allow updating system roles
END;
GO

-- Delete role
CREATE OR ALTER PROCEDURE dbo.usp_DeleteRole
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Remove all user assignments for this role
        DELETE FROM dbo.UserRoles
        WHERE RoleId = @Id;
        
        -- Delete the role
        DELETE FROM dbo.Roles
        WHERE Id = @Id AND IsSystemRole = 0; -- Don't allow deleting system roles
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Rethrow the error
        THROW;
    END CATCH;
END;
GO

-- Assign a role to a user
CREATE OR ALTER PROCEDURE dbo.usp_AssignRoleToUser
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if already assigned
    IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    BEGIN
        INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedAt)
        VALUES (@UserId, @RoleId, GETDATE());
    END
END;
GO

-- Remove a role from a user
CREATE OR ALTER PROCEDURE dbo.usp_RemoveRoleFromUser
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.UserRoles
    WHERE UserId = @UserId AND RoleId = @RoleId;
END;
GO

-- Authenticate a user - returns user details if authentication is successful
CREATE OR ALTER PROCEDURE dbo.usp_AuthenticateUser
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get user with password hash for authentication
    SELECT u.Id, u.Username, u.PasswordHash, u.Salt, u.FirstName, u.LastName, u.Email,
           u.IsActive, u.IsLockedOut, u.FailedLoginAttempts, u.RequiresMFA, u.MustChangePassword
    FROM dbo.Users u
    WHERE u.Username = @Username;
    
    -- Get user roles
    SELECT r.Id, r.Name
    FROM dbo.Roles r
    JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
    JOIN dbo.Users u ON ur.UserId = u.Id
    WHERE u.Username = @Username;
END;
GO

-- Bulk set user roles (removes existing roles and adds new ones)
CREATE OR ALTER PROCEDURE dbo.usp_SetUserRoles
    @UserId INT,
    @RoleIds nvarchar(max) -- Comma-separated list of role IDs
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Delete existing roles for this user
        DELETE FROM dbo.UserRoles
        WHERE UserId = @UserId;
        
        -- Add new roles if any provided
        IF @RoleIds IS NOT NULL AND LEN(@RoleIds) > 0
        BEGIN
            -- Create a table to store the role IDs
            DECLARE @RoleTable TABLE (RoleId INT);
            
            -- Parse the comma-separated list
            INSERT INTO @RoleTable (RoleId)
            SELECT value FROM STRING_SPLIT(@RoleIds, ',');
            
            -- Add the new roles
            INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedAt)
            SELECT @UserId, RoleId, GETDATE()
            FROM @RoleTable;
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Rethrow the error
        THROW;
    END CATCH;
END;
GO

-- Get users by role ID
CREATE OR ALTER PROCEDURE dbo.usp_GetUsersByRoleId
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT u.Id, u.Username, u.FirstName, u.LastName, u.Email, u.Phone, u.IsActive
    FROM dbo.Users u
    JOIN dbo.UserRoles ur ON u.Id = ur.UserId
    WHERE ur.RoleId = @RoleId
    ORDER BY u.Username;
END;
GO
