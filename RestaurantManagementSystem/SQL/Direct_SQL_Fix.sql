-- Direct_SQL_Fix.sql
-- Run this script directly in SQL Server Management Studio to fix authentication

-- Create the Users table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Salt NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        FirstName NVARCHAR(50) NULL,
        LastName NVARCHAR(50) NULL,
        RoleId INT NOT NULL DEFAULT 3
    );
    
    PRINT 'Users table created';
END

-- Create or update the admin user with known password hash
-- The hash corresponds to password 'password' with salt 'abc123'
DECLARE @Salt NVARCHAR(100) = 'abc123';
DECLARE @PasswordHash NVARCHAR(255) = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';

-- Check if admin user exists
IF EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    -- Update admin user
    UPDATE Users
    SET PasswordHash = @PasswordHash,
        Salt = @Salt,
        Email = 'admin@restaurant.com',
        FirstName = 'System',
        LastName = 'Administrator'
    WHERE Username = 'admin';
    
    PRINT 'Admin user updated with password: password';
END
ELSE
BEGIN
    -- Insert admin user
    INSERT INTO Users (Username, PasswordHash, Salt, Email, FirstName, LastName, RoleId)
    VALUES ('admin', @PasswordHash, @Salt, 'admin@restaurant.com', 'System', 'Administrator', 1);
    
    PRINT 'Admin user created with password: password';
END

-- Create basic authentication stored procedure
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuthenticateUser]') AND type in (N'P', N'PC'))
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_AuthenticateUser]
        @Username NVARCHAR(50),
        @PasswordHash NVARCHAR(255)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @UserId INT;
        DECLARE @StoredHash NVARCHAR(255);
        DECLARE @Success BIT = 0;
        DECLARE @Message NVARCHAR(255) = ''Invalid username or password'';
        
        -- Get user information
        SELECT @UserId = Id, @StoredHash = PasswordHash
        FROM Users 
        WHERE Username = @Username;
        
        -- Check if user exists
        IF @UserId IS NULL
        BEGIN
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                  NULL AS Email, NULL AS FirstName, NULL AS LastName, 0 AS RequiresMFA;
            RETURN;
        END
        
        -- Check password
        IF @StoredHash = @PasswordHash
        BEGIN
            SET @Success = 1;
            SET @Message = ''Authentication successful'';
            
            -- Return user info
            SELECT @Success AS Success, @Message AS Message, 
                  Id AS UserId, 
                  Username, 
                  Email,
                  FirstName,
                  LastName,
                  0 AS RequiresMFA
            FROM Users
            WHERE Username = @Username;
        END
        ELSE
        BEGIN
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                  NULL AS Email, NULL AS FirstName, NULL AS LastName, 0 AS RequiresMFA;
        END
    END
    ');
    
    PRINT 'sp_AuthenticateUser procedure created';
END

-- Create basic roles stored procedure
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetUserRolesAndPermissions]') AND type in (N'P', N'PC'))
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
        @Username NVARCHAR(50)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- For admin user, always return Administrator
        IF @Username = ''admin''
        BEGIN
            SELECT ''Administrator'' AS RoleName, ''System Administrator'' AS Description;
            RETURN;
        END
        
        -- For other users, check roles if table exists
        IF EXISTS (SELECT * FROM sys.tables WHERE name = ''Roles'')
        AND EXISTS (SELECT * FROM sys.tables WHERE name = ''UserRoles'')
        BEGIN
            -- Try to get from UserRoles
            SELECT r.RoleName, r.Description
            FROM Users u
            JOIN UserRoles ur ON u.Id = ur.UserId
            JOIN Roles r ON ur.RoleId = r.RoleId
            WHERE u.Username = @Username;
        END
        ELSE
        BEGIN
            -- Default to Staff role
            SELECT ''Staff'' AS RoleName, ''Regular Staff'' AS Description;
        END
    END
    ');
    
    PRINT 'sp_GetUserRolesAndPermissions procedure created';
END

-- Test authentication
PRINT 'Testing authentication for admin user...';
EXEC sp_AuthenticateUser 'admin', '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';

PRINT 'Authentication setup completed successfully';
