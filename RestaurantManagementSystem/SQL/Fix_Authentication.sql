-- Fix_Authentication.sql
-- Script to fix authentication issues

-- First, check if the stored procedure exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuthenticateUser]') AND type in (N'P', N'PC'))
BEGIN
    PRINT 'Creating sp_AuthenticateUser stored procedure';
    
    EXEC('
    CREATE PROCEDURE [dbo].[sp_AuthenticateUser]
        @Username NVARCHAR(50),
        @PasswordHash NVARCHAR(200)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @Success BIT = 0;
        DECLARE @Message NVARCHAR(200) = ''Invalid username or password'';
        DECLARE @UserId INT;
        
        -- Check if the user exists and the password matches
        IF EXISTS (SELECT 1 FROM Users WHERE Username = @Username AND Password = @PasswordHash)
        BEGIN
            SET @Success = 1;
            SET @Message = ''Authentication successful'';
            
            -- Get user details
            SELECT 
                @UserId = Id
            FROM 
                Users 
            WHERE 
                Username = @Username;
                
            -- Update last login time
            UPDATE Users
            SET LastLogin = GETDATE()
            WHERE Id = @UserId;
            
            -- Return user information
            SELECT 
                @Success AS Success,
                @Message AS Message,
                Id AS UserId,
                Username,
                Email,
                FirstName,
                LastName,
                0 AS RequiresMFA -- Default to not requiring MFA
            FROM 
                Users 
            WHERE 
                Id = @UserId;
        END
        ELSE
        BEGIN
            -- Return authentication failure
            SELECT 
                @Success AS Success,
                @Message AS Message,
                NULL AS UserId,
                NULL AS Username,
                NULL AS Email,
                NULL AS FirstName,
                NULL AS LastName,
                0 AS RequiresMFA;
        END
    END
    ')
END
ELSE
BEGIN
    PRINT 'sp_AuthenticateUser stored procedure already exists';
END

-- Add the Salt column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Salt')
BEGIN
    PRINT 'Adding Salt column to Users table';
    ALTER TABLE Users ADD Salt NVARCHAR(50) NULL;
END

-- Check if the GetUserSalt function exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_GetUserSalt]') AND type IN (N'FN', N'IF', N'TF'))
BEGIN
    PRINT 'Creating fn_GetUserSalt function';
    
    EXEC('
    CREATE FUNCTION [dbo].[fn_GetUserSalt](@Username NVARCHAR(50))
    RETURNS NVARCHAR(50)
    AS
    BEGIN
        DECLARE @Salt NVARCHAR(50);
        
        SELECT @Salt = ISNULL(Salt, '''')
        FROM Users
        WHERE Username = @Username;
        
        RETURN @Salt;
    END
    ')
END
ELSE
BEGIN
    PRINT 'fn_GetUserSalt function already exists';
END

-- Reset the admin account to ensure it works
-- First check if a user with username 'admin' exists
IF EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    PRINT 'Updating admin user';
    
    -- Set a known password for the admin user (password: Admin@123)
    -- The actual hash is computed at runtime, this is just a placeholder
    -- In a real scenario, you would compute a proper hash using your hashing algorithm
    UPDATE Users
    SET 
        Password = 'Admin@123',  -- Plain text for now, will be hashed at runtime
        Salt = '',               -- No salt for this basic setup
        IsActive = 1
    WHERE 
        Username = 'admin';
END
ELSE
BEGIN
    PRINT 'Creating admin user';
    
    -- Find the highest role ID (assume it's for admin)
    DECLARE @AdminRoleId INT;
    SELECT @AdminRoleId = ISNULL(MAX(Role), 9) FROM Users;
    
    -- Create the admin user
    INSERT INTO Users (Username, Password, FirstName, LastName, Email, Role, IsActive, Salt)
    VALUES ('admin', 'Admin@123', 'System', 'Administrator', 'admin@restaurant.com', @AdminRoleId, 1, '');
END

PRINT 'Authentication fix script completed successfully.';
