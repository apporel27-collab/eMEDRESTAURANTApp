-- Direct_Admin_Setup.sql
-- This is a simplified script to directly create or update the admin user
-- It inspects the database structure first and adapts to it

-- First, let's check what tables and columns we have
PRINT 'Checking database structure...';

-- Check if Users table exists
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    PRINT 'Users table exists';
    
    -- Get actual column names in Users table
    DECLARE @UserColumns TABLE (ColumnName NVARCHAR(128));
    INSERT INTO @UserColumns
    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users';
    
    -- Check key columns
    DECLARE @HasUserId BIT = 0;
    DECLARE @HasId BIT = 0;
    DECLARE @HasUsername BIT = 0;
    DECLARE @HasPasswordHash BIT = 0;
    DECLARE @HasPassword BIT = 0;
    DECLARE @HasSalt BIT = 0;
    DECLARE @HasRoleId BIT = 0;
    DECLARE @HasEmail BIT = 0;
    
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'UserId') SET @HasUserId = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'Id') SET @HasId = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'Username') SET @HasUsername = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'PasswordHash') SET @HasPasswordHash = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'Password') SET @HasPassword = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'Salt') SET @HasSalt = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'RoleId') SET @HasRoleId = 1;
    IF EXISTS (SELECT 1 FROM @UserColumns WHERE ColumnName = 'Email') SET @HasEmail = 1;
    
    PRINT 'Column check:';
    PRINT 'UserId: ' + CASE WHEN @HasUserId = 1 THEN 'Exists' ELSE 'Missing' END;
    PRINT 'Id: ' + CASE WHEN @HasId = 1 THEN 'Exists' ELSE 'Missing' END;
    PRINT 'Username: ' + CASE WHEN @HasUsername = 1 THEN 'Exists' ELSE 'Missing' END;
    PRINT 'PasswordHash: ' + CASE WHEN @HasPasswordHash = 1 THEN 'Exists' ELSE 'Missing' END;
    PRINT 'Password: ' + CASE WHEN @HasPassword = 1 THEN 'Exists' ELSE 'Missing' END;
    PRINT 'Salt: ' + CASE WHEN @HasSalt = 1 THEN 'Exists' ELSE 'Missing' END;
    
    -- Add missing columns
    IF @HasPasswordHash = 0 AND @HasPassword = 1
    BEGIN
        PRINT 'Renaming Password column to PasswordHash';
        EXEC sp_rename 'Users.Password', 'PasswordHash', 'COLUMN';
        SET @HasPasswordHash = 1;
        SET @HasPassword = 0;
    END
    
    IF @HasPasswordHash = 0 AND @HasPassword = 0
    BEGIN
        PRINT 'Adding PasswordHash column';
        ALTER TABLE Users ADD PasswordHash NVARCHAR(255);
        SET @HasPasswordHash = 1;
    END
    
    IF @HasSalt = 0
    BEGIN
        PRINT 'Adding Salt column';
        ALTER TABLE Users ADD Salt NVARCHAR(100);
        SET @HasSalt = 1;
    END
    
    -- Determine ID column name for use in queries
    DECLARE @IdColumnName NVARCHAR(50) = CASE
        WHEN @HasUserId = 1 THEN 'UserId'
        WHEN @HasId = 1 THEN 'Id'
        ELSE 'Id' -- Default if neither exists
    END;
    
    -- Check if admin user exists
    DECLARE @AdminExists BIT = 0;
    DECLARE @SQL NVARCHAR(MAX) = N'SELECT @exists = CASE WHEN EXISTS (SELECT 1 FROM Users WHERE Username = ''admin'') THEN 1 ELSE 0 END';
    DECLARE @ParmDefinition NVARCHAR(100) = N'@exists BIT OUTPUT';
    DECLARE @Exists BIT;
    
    EXEC sp_executesql @SQL, @ParmDefinition, @exists = @Exists OUTPUT;
    SET @AdminExists = @Exists;
    
    PRINT 'Admin user exists: ' + CASE WHEN @AdminExists = 1 THEN 'Yes' ELSE 'No' END;
    
    -- Set values for admin user
    DECLARE @Salt NVARCHAR(100) = 'abc123';
    DECLARE @PasswordHash NVARCHAR(255) = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918';
    
    -- Update or insert admin user
    IF @AdminExists = 1
    BEGIN
        -- Create update SQL based on available columns
        SET @SQL = N'UPDATE Users SET ';
        
        -- Always update these critical columns
        SET @SQL = @SQL + N'Username = ''admin'', ';
        SET @SQL = @SQL + N'PasswordHash = @PasswordHash, ';
        SET @SQL = @SQL + N'Salt = @Salt';
        
        -- Add optional columns conditionally
        IF @HasEmail = 1
            SET @SQL = @SQL + N', Email = ''admin@restaurant.com''';
        
        -- Add WHERE condition
        SET @SQL = @SQL + N' WHERE Username = ''admin''';
        
        PRINT 'Executing update:';
        PRINT @SQL;
        
        EXEC sp_executesql @SQL, N'@PasswordHash NVARCHAR(255), @Salt NVARCHAR(100)', 
            @PasswordHash = @PasswordHash, @Salt = @Salt;
            
        PRINT 'Admin user updated with correct credentials';
    END
    ELSE
    BEGIN
        -- Create insert SQL based on available columns
        SET @SQL = N'INSERT INTO Users (';
        DECLARE @ValueSQL NVARCHAR(MAX) = N' VALUES (';
        
        -- Always insert these critical columns
        SET @SQL = @SQL + N'Username, PasswordHash, Salt';
        SET @ValueSQL = @ValueSQL + N'''admin'', @PasswordHash, @Salt';
        
        -- Add optional columns conditionally
        IF @HasEmail = 1
        BEGIN
            SET @SQL = @SQL + N', Email';
            SET @ValueSQL = @ValueSQL + N', ''admin@restaurant.com''';
        END
        
        IF @HasRoleId = 1
        BEGIN
            SET @SQL = @SQL + N', RoleId';
            SET @ValueSQL = @ValueSQL + N', 1'; -- Assuming 1 is admin role ID
        END
        
        -- Close the SQL statement
        SET @SQL = @SQL + N')' + @ValueSQL + N')';
        
        PRINT 'Executing insert:';
        PRINT @SQL;
        
        EXEC sp_executesql @SQL, N'@PasswordHash NVARCHAR(255), @Salt NVARCHAR(100)', 
            @PasswordHash = @PasswordHash, @Salt = @Salt;
            
        PRINT 'Admin user created with correct credentials';
    END
    
    -- Verify user exists and has correct hash format
    PRINT 'Verifying admin user...';
    
    -- Check if sp_AuthenticateUser stored procedure exists
    IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_AuthenticateUser]') AND type in (N'P', N'PC'))
    BEGIN
        PRINT 'Testing authentication with sp_AuthenticateUser...';
        EXEC sp_AuthenticateUser 'admin', @PasswordHash;
    END
    ELSE
    BEGIN
        PRINT 'Stored procedure sp_AuthenticateUser does not exist';
        PRINT 'Creating simplified authentication procedure...';
        
        -- Create a simplified stored procedure
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
            SELECT @UserId = CASE 
                    WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'') THEN Id
                    WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''UserId'') THEN UserId
                    ELSE NULL
                END, 
                @StoredHash = PasswordHash 
            FROM Users 
            WHERE Username = @Username;
            
            -- Check if user exists
            IF @UserId IS NULL
            BEGIN
                SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                      NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
                RETURN;
            END
            
            -- Check password
            IF @StoredHash = @PasswordHash
            BEGIN
                SET @Success = 1;
                SET @Message = ''Authentication successful'';
                
                -- Return user info
                SELECT @Success AS Success, @Message AS Message, 
                      CASE 
                          WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Id'') THEN Id
                          WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''UserId'') THEN UserId
                      END AS UserId, 
                      Username,
                      CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Users'' AND COLUMN_NAME = ''Email'') 
                          THEN Email ELSE NULL END AS Email,
                      ''Admin'' AS FirstName,
                      ''User'' AS LastName,
                      0 AS RequiresMFA
                FROM Users
                WHERE Username = @Username;
            END
            ELSE
            BEGIN
                SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                      NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
            END
        END
        ');
        
        PRINT 'Testing authentication with new sp_AuthenticateUser...';
        EXEC sp_AuthenticateUser 'admin', @PasswordHash;
    END
    
    -- Check if sp_GetUserRolesAndPermissions stored procedure exists
    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetUserRolesAndPermissions]') AND type in (N'P', N'PC'))
    BEGIN
        PRINT 'Creating sp_GetUserRolesAndPermissions stored procedure...';
        
        EXEC('
        CREATE PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
            @Username NVARCHAR(50)
        AS
        BEGIN
            SET NOCOUNT ON;
            
            -- Always return Administrator role for admin user
            IF @Username = ''admin''
            BEGIN
                SELECT ''Administrator'' AS RoleName;
                RETURN;
            END
            
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
                -- Default to lowest role if no proper structure exists
                SELECT ''Staff'' as RoleName;
            END
        END
        ');
        
        PRINT 'sp_GetUserRolesAndPermissions created';
    END
    
    -- Show the final admin user details
    SET @SQL = N'SELECT Username, PasswordHash, Salt FROM Users WHERE Username = ''admin''';
    EXEC sp_executesql @SQL;
    
    PRINT 'Admin setup completed successfully';
END
ELSE
BEGIN
    -- Users table doesn't exist, create it
    PRINT 'Users table does not exist. Creating it...';
    
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Email NVARCHAR(100) NULL,
        Salt NVARCHAR(100) NOT NULL,
        RoleId INT NULL
    );
    
    PRINT 'Users table created';
    
    -- Insert admin user
    INSERT INTO Users (Username, PasswordHash, Email, Salt, RoleId)
    VALUES ('admin', '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 
            'admin@restaurant.com', 'abc123', 1);
    
    PRINT 'Admin user created with correct credentials';
    
    -- Create basic stored procedures
    PRINT 'Creating authentication stored procedures...';
    
    -- Create authentication stored procedure
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
        SELECT @UserId = Id, 
               @StoredHash = PasswordHash 
        FROM Users 
        WHERE Username = @Username;
        
        -- Check if user exists
        IF @UserId IS NULL
        BEGIN
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                  NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
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
                  ''Admin'' AS FirstName,
                  ''User'' AS LastName,
                  0 AS RequiresMFA
            FROM Users
            WHERE Username = @Username;
        END
        ELSE
        BEGIN
            SELECT @Success AS Success, @Message AS Message, NULL AS UserId, NULL AS Username, 
                  NULL AS Email, NULL AS FirstName, NULL AS LastName, NULL AS RequiresMFA;
        END
    END
    ');
    
    -- Create roles and permissions stored procedure
    EXEC('
    CREATE PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
        @Username NVARCHAR(50)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- For simplicity, always return Administrator for admin user
        IF @Username = ''admin''
        BEGIN
            SELECT ''Administrator'' AS RoleName;
        END
        ELSE
        BEGIN
            -- Default to staff role for other users
            SELECT ''Staff'' AS RoleName;
        END
    END
    ');
    
    PRINT 'Stored procedures created';
    PRINT 'Admin setup completed successfully';
END
