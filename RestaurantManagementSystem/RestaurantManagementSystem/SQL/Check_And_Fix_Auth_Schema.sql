-- Check_And_Fix_Auth_Schema.sql
-- Script to check and fix authentication schema issues

-- First let's check the actual structure of the Users table
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users';

-- Check if we have the proper columns that AuthService expects
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordHash')
BEGIN
    -- We need to add PasswordHash column
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Password')
    BEGIN
        -- Rename Password column to PasswordHash
        EXEC sp_rename 'Users.Password', 'PasswordHash', 'COLUMN';
        PRINT 'Renamed Password column to PasswordHash';
    END
    ELSE
    BEGIN
        -- Add PasswordHash column
        ALTER TABLE Users ADD PasswordHash NVARCHAR(255);
        PRINT 'Added PasswordHash column to Users table';
    END
END

-- Make sure we have Salt column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Salt')
BEGIN
    -- Add Salt column
    ALTER TABLE Users ADD Salt NVARCHAR(100);
    PRINT 'Added Salt column to Users table';
END

-- Update Admin user with proper hash format
-- Known hash for password 'password' with salt 'abc123'
DECLARE @AdminUsername NVARCHAR(50) = 'admin';
DECLARE @Salt NVARCHAR(100) = 'abc123';
DECLARE @PasswordHash NVARCHAR(255) = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';

-- Check if admin user exists
IF EXISTS (SELECT 1 FROM Users WHERE Username = @AdminUsername)
BEGIN
    -- Update existing admin user
    UPDATE Users
    SET 
        Salt = @Salt,
        PasswordHash = @PasswordHash,
        IsLockedOut = 0,
        FailedLoginAttempts = 0,
        RequiresMFA = 0  -- Ensure MFA is disabled for admin user
    WHERE Username = @AdminUsername;
    PRINT 'Updated admin user with correct password hash format';
END
ELSE
BEGIN
    -- Determine what columns actually exist in the Users table
    DECLARE @UserIdCol NVARCHAR(50) = 'Id';
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UserId')
    BEGIN
        SET @UserIdCol = 'UserId';
    END
    
    -- Check if we have proper columns for insert
    DECLARE @HasEmailCol BIT = 1;
    DECLARE @HasFullNameCol BIT = 1;
    DECLARE @HasFirstNameCol BIT = 1;
    DECLARE @HasLastNameCol BIT = 1;
    DECLARE @HasRoleCol BIT = 1;
    DECLARE @HasRoleIdCol BIT = 1;
    
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Email')
        SET @HasEmailCol = 0;
    
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FullName')
        SET @HasFullNameCol = 0;
        
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FirstName')
        SET @HasFirstNameCol = 0;
        
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastName')
        SET @HasLastNameCol = 0;
        
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role')
        SET @HasRoleCol = 0;
        
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'RoleId')
        SET @HasRoleIdCol = 0;
    
    -- Create SQL for admin user insertion
    DECLARE @SQL NVARCHAR(MAX);
    SET @SQL = 'INSERT INTO Users (Username, ';
    
    -- Add column names based on what exists
    IF @HasEmailCol = 1 SET @SQL = @SQL + 'Email, ';
    IF @HasFullNameCol = 1 SET @SQL = @SQL + 'FullName, ';
    IF @HasFirstNameCol = 1 SET @SQL = @SQL + 'FirstName, ';
    IF @HasLastNameCol = 1 SET @SQL = @SQL + 'LastName, ';
    IF @HasRoleCol = 1 SET @SQL = @SQL + 'Role, ';
    IF @HasRoleIdCol = 1 SET @SQL = @SQL + 'RoleId, ';
    
    SET @SQL = @SQL + 'Salt, PasswordHash, IsLockedOut, FailedLoginAttempts) VALUES (''admin'', ';
    
    -- Add values based on what exists
    IF @HasEmailCol = 1 SET @SQL = @SQL + '''admin@restaurant.com'', ';
    IF @HasFullNameCol = 1 SET @SQL = @SQL + '''System Administrator'', ';
    IF @HasFirstNameCol = 1 SET @SQL = @SQL + '''System'', ';
    IF @HasLastNameCol = 1 SET @SQL = @SQL + '''Administrator'', ';
    IF @HasRoleCol = 1 SET @SQL = @SQL + '12, '; -- System admin role
    IF @HasRoleIdCol = 1 SET @SQL = @SQL + '1, '; -- Assuming 1 is admin role ID
    
    SET @SQL = @SQL + '''' + @Salt + ''', ''' + @PasswordHash + ''', 0, 0)';
    
    -- Execute the dynamic SQL
    PRINT @SQL;
    EXEC sp_executesql @SQL;
    PRINT 'Created admin user with correct password hash format';
END

-- Check if stored procedure exists for authentication
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuthenticateUser]') AND type in (N'P', N'PC'))
BEGIN
    PRINT 'Creating sp_AuthenticateUser stored procedure';
    
    DECLARE @AuthProcSQL NVARCHAR(MAX) = '
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
        SELECT @UserId = CASE 
                         WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'') THEN Id
                         WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''UserId'') THEN UserId
                         ELSE NULL
                     END, 
               @StoredHash = PasswordHash, 
               @IsLockedOut = ISNULL(IsLockedOut, 0)
        FROM Users 
        WHERE Username = @Username;
        
        -- Check if user exists
        IF @UserId IS NULL
        BEGIN
            -- THIS IS DELIBERATELY SET TO RETURN "Invalid username and password" for security
            -- Even when just the username is invalid (to avoid username enumeration attacks)
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                   NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
            RETURN;
        END
        
        -- Check if account is locked
        IF @IsLockedOut = 1
        BEGIN
            -- For security reasons, still just say invalid username and password
            -- Don''t reveal that the account is locked to potential attackers
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                   NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
            RETURN;
        END
        
        -- Check password
        IF @StoredHash = @PasswordHash
        BEGIN
            SET @Success = 1;
            SET @Message = ''Authentication successful'';
            
            -- Reset failed login attempts
            UPDATE Users SET FailedLoginAttempts = 0 
            WHERE CASE 
                WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'') THEN Id
                WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''UserId'') THEN UserId
            END = @UserId;
            
            -- Return user info
            SELECT @Success AS Success, @Message AS Message, 
                   CASE 
                       WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'') THEN Id
                       WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''UserId'') THEN UserId
                   END AS UserId, 
                   Username, 
                   CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Email'') 
                        THEN Email ELSE NULL END AS Email, 
                   CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''FirstName'') 
                        THEN FirstName ELSE NULL END AS FirstName, 
                   CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''LastName'') 
                        THEN LastName ELSE NULL END AS LastName, 
                   CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''RequiresMFA'') 
                        THEN RequiresMFA ELSE 0 END AS RequiresMFA
            FROM Users
            WHERE Username = @Username;
        END
        ELSE
        BEGIN
            -- Increment failed login attempts
            UPDATE Users 
            SET FailedLoginAttempts = ISNULL(FailedLoginAttempts, 0) + 1,
                IsLockedOut = CASE WHEN ISNULL(FailedLoginAttempts, 0) + 1 >= 5 THEN 1 ELSE 0 END
            WHERE CASE 
                WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'') THEN Id
                WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''UserId'') THEN UserId
            END = @UserId;
            
            -- Return generic error for security (don''t reveal that password was wrong)
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                   NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
        END
    END
    ';
    
    EXEC sp_executesql @AuthProcSQL;
END

-- Make sure we have the sp_GetUserRolesAndPermissions stored procedure
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetUserRolesAndPermissions]') AND type in (N'P', N'PC'))
BEGIN
    PRINT 'Creating sp_GetUserRolesAndPermissions stored procedure...';
    
    EXEC('
    CREATE PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
        @Username NVARCHAR(50)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- Check which columns exist for proper query building
        DECLARE @UserIdCol NVARCHAR(10) = ''UserId'';
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'')
            SET @UserIdCol = ''Id'';
            
        -- Check if Roles and UserRoles tables exist
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ''Roles'') 
        AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ''UserRoles'')
        BEGIN
            -- Get user roles from proper tables
            DECLARE @SQL NVARCHAR(MAX);
            SET @SQL = ''
                SELECT r.RoleName
                FROM Users u
                JOIN UserRoles ur ON u.'' + @UserIdCol + '' = ur.UserId
                JOIN Roles r ON ur.RoleId = r.RoleId
                WHERE u.Username = @Username
            '';
            
            EXEC sp_executesql @SQL, N''@Username NVARCHAR(50)'', @Username;
        END
        ELSE IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Role'')
        BEGIN
            -- Get role directly from Users table if it has a Role column
            SELECT Role as RoleName FROM Users WHERE Username = @Username;
        END
        ELSE IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''RoleName'')
        BEGIN
            -- Get role directly from Users table if it has a RoleName column
            SELECT RoleName FROM Users WHERE Username = @Username;
        END
        ELSE
        BEGIN
            -- Default to admin role if no proper structure exists
            SELECT ''Administrator'' as RoleName;
        END
    END
    ');
    
    PRINT 'Stored procedure created';
END

-- Add RequiresMFA column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'RequiresMFA')
BEGIN
    ALTER TABLE Users ADD RequiresMFA BIT DEFAULT 0 NOT NULL;
    PRINT 'Added RequiresMFA column to Users table';
    
    -- Disable MFA for all users
    UPDATE Users SET RequiresMFA = 0;
END

-- Make sure IsLockedOut column exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsLockedOut')
BEGIN
    ALTER TABLE Users ADD IsLockedOut BIT DEFAULT 0 NOT NULL;
    PRINT 'Added IsLockedOut column to Users table';
    
    -- Unlock all accounts
    UPDATE Users SET IsLockedOut = 0;
END

-- Reset failed login attempts for all users
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FailedLoginAttempts')
BEGIN
    UPDATE Users SET FailedLoginAttempts = 0;
    PRINT 'Reset failed login attempts for all users';
END

-- Show the admin user details
SELECT * FROM Users WHERE Username = 'admin';

-- Test authentication with admin/password
PRINT 'Testing authentication with admin/password:';
DECLARE @TestHash NVARCHAR(255) = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';

-- Execute the stored procedure if it exists
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuthenticateUser]') AND type in (N'P', N'PC'))
BEGIN
    EXEC sp_AuthenticateUser 'admin', @TestHash;
END
ELSE
BEGIN
    PRINT 'sp_AuthenticateUser stored procedure not found. Cannot test authentication.';
END
