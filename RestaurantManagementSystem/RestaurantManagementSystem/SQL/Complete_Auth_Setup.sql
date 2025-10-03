-- Complete_Auth_Setup.sql
-- Comprehensive script to set up all authentication tables and create admin user

-- Disable foreign key constraints for the database session
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
PRINT 'Disabled foreign key constraints';

-- Drop and recreate Users table
PRINT 'Setting up Users table...';
-- First drop UserRoles if it exists (because of foreign key constraint)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    DROP TABLE UserRoles;
    PRINT 'Dropped existing UserRoles table';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    -- Create a backup of the Users table if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users_Backup')
    BEGIN
        SELECT * INTO Users_Backup FROM Users;
        PRINT 'Users table backed up to Users_Backup';
    END
    
    DROP TABLE Users;
    PRINT 'Dropped existing Users table';
END

-- Create the Users table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Salt NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NULL,
    FirstName NVARCHAR(50) NULL,
    LastName NVARCHAR(50) NULL,
    FullName AS (FirstName + ' ' + LastName) PERSISTED,
    RoleId INT NOT NULL DEFAULT 3, -- Default to Staff role
    IsLockedOut BIT NOT NULL DEFAULT 0,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    RequiresMFA BIT NOT NULL DEFAULT 0,
    LastLoginDate DATETIME NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedDate DATETIME NULL,
    PasswordLastChanged DATETIME NULL,
    MustChangePassword BIT NOT NULL DEFAULT 0
);
PRINT 'Users table created';

-- Create the Roles table
PRINT 'Setting up Roles table...';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    DROP TABLE Roles;
    PRINT 'Dropped existing Roles table';
END

CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
PRINT 'Roles table created';

-- Insert default roles
INSERT INTO Roles (RoleName, Description)
VALUES 
    ('Administrator', 'System administrator with full access'),
    ('Manager', 'Restaurant manager with elevated privileges'),
    ('Staff', 'Regular staff with basic access');
PRINT 'Default roles inserted';

-- Create admin user with hardcoded password 'password'
PRINT 'Creating admin user...';
DECLARE @Salt NVARCHAR(100) = 'abc123';
DECLARE @PasswordHash NVARCHAR(255) = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';

INSERT INTO Users (
    Username, 
    PasswordHash,
    Salt,
    Email,
    FirstName,
    LastName,
    RoleId,
    IsLockedOut,
    FailedLoginAttempts,
    RequiresMFA,
    CreatedDate,
    LastModifiedDate
)
VALUES (
    'admin',
    @PasswordHash,
    @Salt,
    'admin@restaurant.com',
    'System',
    'Administrator',
    1, -- Admin role
    0,  -- Not locked
    0,  -- No failed attempts
    0,  -- MFA not required
    GETDATE(),
    GETDATE()
);
PRINT 'Admin user created';

-- Create UserRoles table for many-to-many relationship between Users and Roles
PRINT 'Setting up UserRoles table...';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    DROP TABLE UserRoles;
    PRINT 'Dropped existing UserRoles table';
END

CREATE TABLE UserRoles (
    UserRoleId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    CONSTRAINT UK_UserRoles_UserRole UNIQUE (UserId, RoleId)
);
PRINT 'UserRoles table created';

-- Insert user role for admin
INSERT INTO UserRoles (UserId, RoleId)
VALUES (1, 1); -- Admin user with Administrator role
PRINT 'Admin user role assigned';

-- Create stored procedures for authentication
PRINT 'Creating authentication stored procedures...';

-- Authentication stored procedure
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuthenticateUser]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_AuthenticateUser];
    PRINT 'Dropped existing sp_AuthenticateUser procedure';
END

EXEC('
CREATE PROCEDURE [dbo].[sp_AuthenticateUser]
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserId INT;
    DECLARE @StoredHash NVARCHAR(255);
    DECLARE @IsLockedOut BIT;
    DECLARE @Success BIT = 0;
    DECLARE @Message NVARCHAR(255) = ''Invalid username or password'';
    
    -- Get user information
    SELECT 
        @UserId = Id, 
        @StoredHash = PasswordHash, 
        @IsLockedOut = IsLockedOut
    FROM Users 
    WHERE Username = @Username;
    
    -- Check if user exists
    IF @UserId IS NULL
    BEGIN
        -- For security, don''t reveal that the username doesn''t exist
        SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
               NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
        RETURN;
    END
    
    -- Check if account is locked
    IF @IsLockedOut = 1
    BEGIN
        -- For security, don''t reveal that the account is locked
        SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
               NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
        RETURN;
    END
    
    -- Check password
    IF @StoredHash = @PasswordHash
    BEGIN
        SET @Success = 1;
        SET @Message = ''Authentication successful'';
        
        -- Reset failed login attempts and update last login date
        UPDATE Users 
        SET 
            FailedLoginAttempts = 0,
            LastLoginDate = GETDATE()
        WHERE Id = @UserId;
        
        -- Return user info
        SELECT 
            @Success AS Success, 
            @Message AS Message, 
            Id AS UserId, 
            Username, 
            Email,
            FirstName,
            LastName,
            RequiresMFA
        FROM Users
        WHERE Id = @UserId;
    END
    ELSE
    BEGIN
        -- Increment failed login attempts
        UPDATE Users 
        SET 
            FailedLoginAttempts = ISNULL(FailedLoginAttempts, 0) + 1,
            IsLockedOut = CASE WHEN ISNULL(FailedLoginAttempts, 0) + 1 >= 5 THEN 1 ELSE 0 END
        WHERE Id = @UserId;
        
        -- For security, don''t reveal that the password was wrong
        SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
               NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
    END
END
');
PRINT 'sp_AuthenticateUser procedure created';

-- Get user roles and permissions
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetUserRolesAndPermissions]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_GetUserRolesAndPermissions];
    PRINT 'Dropped existing sp_GetUserRolesAndPermissions procedure';
END

EXEC('
CREATE PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get user roles
    SELECT 
        r.RoleName,
        r.Description
    FROM Users u
    JOIN UserRoles ur ON u.Id = ur.UserId
    JOIN Roles r ON ur.RoleId = r.RoleId
    WHERE u.Username = @Username;
    
    -- For now, we''re not implementing permissions, but in a real app
    -- this would be the second result set with permissions
END
');
PRINT 'sp_GetUserRolesAndPermissions procedure created';

-- Verify admin user details
PRINT 'Verifying admin user...';
SELECT 
    u.Username, 
    u.PasswordHash, 
    u.Salt, 
    u.Email, 
    u.FirstName, 
    u.LastName,
    u.IsLockedOut,
    u.FailedLoginAttempts,
    r.RoleName
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.Username = 'admin';

-- Test authentication
PRINT 'Testing authentication with admin/password:';
DECLARE @TestHash NVARCHAR(255) = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';
EXEC sp_AuthenticateUser 'admin', @TestHash;

-- Re-enable foreign key constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
PRINT 'Re-enabled foreign key constraints';

PRINT 'Authentication setup completed successfully';
