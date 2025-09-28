-- Create_User_Management_SPs.sql
-- This file creates all stored procedures needed for User and Role management
-- and authentication using dbo.Users, dbo.Roles, and dbo.UserRoles tables

-- Ensure all tables exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [Salt] NVARCHAR(100) NULL, -- Salt may be stored within PasswordHash using format iterations:salt:hash
        [FirstName] NVARCHAR(50) NOT NULL,
        [LastName] NVARCHAR(50) NULL,
        [Email] NVARCHAR(100) NULL,
        [Phone] NVARCHAR(20) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsLockedOut] BIT NOT NULL DEFAULT 0,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LastLoginDate] DATETIME NULL,
        [RequiresMFA] BIT NOT NULL DEFAULT 0,
        [MustChangePassword] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedBy] INT NULL,
        [UpdatedBy] INT NULL
    );
    PRINT 'Created Users table.';
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(50) NOT NULL UNIQUE,
        [Description] NVARCHAR(200) NULL,
        [IsSystemRole] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Created Roles table.';

    -- Insert default roles
    INSERT INTO [dbo].[Roles] ([Name], [Description], [IsSystemRole])
    VALUES
        ('Administrator', 'System Administrator with full access', 1),
        ('Manager', 'Restaurant Manager', 0),
        ('Waiter', 'Table Service Staff', 0),
        ('Chef', 'Kitchen Staff', 0),
        ('Receptionist', 'Front Desk Staff', 0),
        ('Cashier', 'Payment Handling Staff', 0),
        ('User', 'Standard User', 0),
        ('Guest', 'Limited Access Guest', 0);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [Roles]([Id]),
        CONSTRAINT [UQ_UserRoles_UserRole] UNIQUE ([UserId], [RoleId])
    );
    PRINT 'Created UserRoles table.';
END

-- Create admin user if none exists
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    -- Default password: Admin@123
    -- Using PBKDF2 with 10000 iterations, salt and hash
    DECLARE @DefaultHash NVARCHAR(255) = '10000:U2FsdGVkX1/Jlm+CcPfP2g==:jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=';
    
    INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [FirstName], [LastName], [Email], [IsActive])
    VALUES ('admin', @DefaultHash, 'System', 'Administrator', 'admin@restaurant.com', 1);
    
    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    DECLARE @AdminRoleId INT;
    
    SELECT @AdminRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Administrator';
    
    IF @AdminRoleId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
        VALUES (@AdminUserId, @AdminRoleId);
    END
    
    PRINT 'Created admin user and assigned Administrator role.';
END

-- Drop existing stored procedures if they exist
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetUsersList]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetUsersList];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetUserById]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetUserById];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CreateUser]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_CreateUser];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateUser]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_UpdateUser];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteUser]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_DeleteUser];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_AuthenticateUser]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_AuthenticateUser];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetUserRoles]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetUserRoles];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetAllRoles]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetAllRoles];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetRoleById]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_GetRoleById];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CreateRole]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_CreateRole];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateRole]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_UpdateRole];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteRole]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_DeleteRole];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_AssignRoleToUser]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_AssignRoleToUser];

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_RemoveRoleFromUser]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[usp_RemoveRoleFromUser];

-- 1. Procedure to get list of all users with their roles
GO
CREATE PROCEDURE [dbo].[usp_GetUsersList]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.[Id],
        u.[Username],
        u.[FirstName],
        u.[LastName],
        u.[Email],
        u.[Phone],
        u.[IsActive],
        u.[IsLockedOut],
        u.[LastLoginDate],
        STRING_AGG(r.[Name], ', ') AS Roles
    FROM 
        [dbo].[Users] u
    LEFT JOIN 
        [dbo].[UserRoles] ur ON u.Id = ur.UserId
    LEFT JOIN 
        [dbo].[Roles] r ON ur.RoleId = r.Id
    GROUP BY 
        u.[Id],
        u.[Username],
        u.[FirstName],
        u.[LastName],
        u.[Email],
        u.[Phone],
        u.[IsActive],
        u.[IsLockedOut],
        u.[LastLoginDate]
    ORDER BY 
        u.[Username];
END
GO

-- 2. Procedure to get a single user by ID
CREATE PROCEDURE [dbo].[usp_GetUserById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.[Id],
        u.[Username],
        u.[FirstName],
        u.[LastName],
        u.[Email],
        u.[Phone],
        u.[IsActive],
        u.[IsLockedOut],
        u.[RequiresMFA],
        u.[MustChangePassword],
        u.[LastLoginDate]
    FROM 
        [dbo].[Users] u
    WHERE 
        u.[Id] = @Id;
        
    -- Get user roles in a separate result set
    SELECT 
        r.[Id] AS RoleId,
        r.[Name] AS RoleName
    FROM 
        [dbo].[Roles] r
    INNER JOIN 
        [dbo].[UserRoles] ur ON r.Id = ur.RoleId
    WHERE 
        ur.UserId = @Id;
END
GO

-- 3. Procedure to create a new user
CREATE PROCEDURE [dbo].[usp_CreateUser]
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255),
    @Salt NVARCHAR(100) = NULL,
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT = 1,
    @RequiresMFA BIT = 0,
    @MustChangePassword BIT = 0,
    @RoleIds NVARCHAR(MAX) = NULL, -- Comma-separated role IDs
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if username already exists
        IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = @Username)
        BEGIN
            ROLLBACK;
            RAISERROR('Username already exists', 16, 1);
            RETURN;
        END
        
        -- Insert the new user
        INSERT INTO [dbo].[Users] (
            [Username], 
            [PasswordHash], 
            [Salt],
            [FirstName], 
            [LastName], 
            [Email], 
            [Phone], 
            [IsActive],
            [RequiresMFA],
            [MustChangePassword],
            [CreatedBy]
        )
        VALUES (
            @Username, 
            @PasswordHash,
            @Salt, 
            @FirstName, 
            @LastName, 
            @Email, 
            @Phone, 
            @IsActive,
            @RequiresMFA,
            @MustChangePassword,
            @CreatedBy
        );
        
        DECLARE @UserId INT = SCOPE_IDENTITY();
        
        -- Assign roles if provided
        IF @RoleIds IS NOT NULL AND @RoleIds != ''
        BEGIN
            CREATE TABLE #TempRoles (RoleId INT);
            
            -- Parse the comma-separated role IDs
            INSERT INTO #TempRoles (RoleId)
            SELECT CAST(value AS INT) FROM STRING_SPLIT(@RoleIds, ',');
            
            -- Assign each role to the user
            INSERT INTO [dbo].[UserRoles] (UserId, RoleId)
            SELECT @UserId, RoleId FROM #TempRoles
            WHERE RoleId IN (SELECT Id FROM [dbo].[Roles]);
            
            DROP TABLE #TempRoles;
        END
        
        COMMIT;
        
        SELECT @UserId AS UserId, 'User created successfully' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 4. Procedure to update an existing user
CREATE PROCEDURE [dbo].[usp_UpdateUser]
    @Id INT,
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255) = NULL, -- NULL means don't update password
    @Salt NVARCHAR(100) = NULL,
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT = 1,
    @IsLockedOut BIT = NULL,
    @RequiresMFA BIT = NULL,
    @MustChangePassword BIT = NULL,
    @RoleIds NVARCHAR(MAX) = NULL, -- Comma-separated role IDs
    @UpdatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @Id)
        BEGIN
            ROLLBACK;
            RAISERROR('User not found', 16, 1);
            RETURN;
        END
        
        -- Check if username is taken by another user
        IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = @Username AND [Id] != @Id)
        BEGIN
            ROLLBACK;
            RAISERROR('Username already taken by another user', 16, 1);
            RETURN;
        END
        
        -- Update user info
        UPDATE [dbo].[Users]
        SET 
            [Username] = @Username,
            [FirstName] = @FirstName,
            [LastName] = @LastName,
            [Email] = @Email,
            [Phone] = @Phone,
            [IsActive] = @IsActive,
            [IsLockedOut] = ISNULL(@IsLockedOut, [IsLockedOut]),
            [RequiresMFA] = ISNULL(@RequiresMFA, [RequiresMFA]),
            [MustChangePassword] = ISNULL(@MustChangePassword, [MustChangePassword]),
            [UpdatedAt] = GETDATE(),
            [UpdatedBy] = @UpdatedBy
        WHERE 
            [Id] = @Id;
            
        -- Update password if provided
        IF @PasswordHash IS NOT NULL
        BEGIN
            UPDATE [dbo].[Users]
            SET 
                [PasswordHash] = @PasswordHash,
                [Salt] = @Salt
            WHERE 
                [Id] = @Id;
        END
        
        -- Update roles if provided
        IF @RoleIds IS NOT NULL
        BEGIN
            -- Remove existing roles
            DELETE FROM [dbo].[UserRoles]
            WHERE [UserId] = @Id;
            
            -- Add new roles if not empty string
            IF @RoleIds != ''
            BEGIN
                CREATE TABLE #TempRoles (RoleId INT);
                
                -- Parse the comma-separated role IDs
                INSERT INTO #TempRoles (RoleId)
                SELECT CAST(value AS INT) FROM STRING_SPLIT(@RoleIds, ',');
                
                -- Assign each role to the user
                INSERT INTO [dbo].[UserRoles] (UserId, RoleId)
                SELECT @Id, RoleId FROM #TempRoles
                WHERE RoleId IN (SELECT Id FROM [dbo].[Roles]);
                
                DROP TABLE #TempRoles;
            END
        END
        
        COMMIT;
        
        SELECT 'User updated successfully' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 5. Procedure to delete a user
CREATE PROCEDURE [dbo].[usp_DeleteUser]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @Id)
        BEGIN
            ROLLBACK;
            RAISERROR('User not found', 16, 1);
            RETURN;
        END
        
        -- Delete user roles first
        DELETE FROM [dbo].[UserRoles]
        WHERE [UserId] = @Id;
        
        -- Delete the user
        DELETE FROM [dbo].[Users]
        WHERE [Id] = @Id;
        
        COMMIT;
        
        SELECT 'User deleted successfully' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 6. Procedure to authenticate a user
CREATE PROCEDURE [dbo].[usp_AuthenticateUser]
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserId INT;
    DECLARE @IsLockedOut BIT;
    DECLARE @FailedAttempts INT;
    
    -- Get user information
    SELECT 
        @UserId = [Id],
        @IsLockedOut = [IsLockedOut],
        @FailedAttempts = [FailedLoginAttempts]
    FROM 
        [dbo].[Users]
    WHERE 
        [Username] = @Username;
        
    -- Check if user exists
    IF @UserId IS NULL
    BEGIN
        SELECT
            0 AS Success,
            'Invalid username or password' AS Message,
            NULL AS UserId,
            NULL AS Username,
            NULL AS Email,
            NULL AS FirstName,
            NULL AS LastName,
            CAST(0 AS BIT) AS RequiresMFA;
        RETURN;
    END
    
    -- Check if account is locked
    IF @IsLockedOut = 1
    BEGIN
        SELECT
            0 AS Success,
            'This account is locked. Please contact an administrator.' AS Message,
            NULL AS UserId,
            NULL AS Username,
            NULL AS Email,
            NULL AS FirstName,
            NULL AS LastName,
            CAST(0 AS BIT) AS RequiresMFA;
        RETURN;
    END
    
    -- Check if password matches
    DECLARE @StoredHash NVARCHAR(255);
    
    SELECT @StoredHash = [PasswordHash]
    FROM [dbo].[Users]
    WHERE [Id] = @UserId;
    
    IF @PasswordHash = @StoredHash -- Comparing hashes
    BEGIN
        -- Successful login - reset failed attempts and update last login date
        UPDATE [dbo].[Users]
        SET 
            [FailedLoginAttempts] = 0,
            [LastLoginDate] = GETDATE()
        WHERE 
            [Id] = @UserId;
            
        -- Return user details
        SELECT
            1 AS Success,
            'Authentication successful' AS Message,
            u.[Id] AS UserId,
            u.[Username],
            u.[Email],
            u.[FirstName],
            u.[LastName],
            u.[RequiresMFA]
        FROM 
            [dbo].[Users] u
        WHERE 
            u.[Id] = @UserId;
    END
    ELSE
    BEGIN
        -- Failed login attempt - increment counter
        SET @FailedAttempts = @FailedAttempts + 1;
        
        -- Lock account after 5 failed attempts
        IF @FailedAttempts >= 5
        BEGIN
            UPDATE [dbo].[Users]
            SET 
                [FailedLoginAttempts] = @FailedAttempts,
                [IsLockedOut] = 1
            WHERE 
                [Id] = @UserId;
                
            SELECT
                0 AS Success,
                'This account has been locked due to too many failed login attempts.' AS Message,
                NULL AS UserId,
                NULL AS Username,
                NULL AS Email,
                NULL AS FirstName,
                NULL AS LastName,
                CAST(0 AS BIT) AS RequiresMFA;
        END
        ELSE
        BEGIN
            UPDATE [dbo].[Users]
            SET 
                [FailedLoginAttempts] = @FailedAttempts
            WHERE 
                [Id] = @UserId;
                
            SELECT
                0 AS Success,
                'Invalid username or password' AS Message,
                NULL AS UserId,
                NULL AS Username,
                NULL AS Email,
                NULL AS FirstName,
                NULL AS LastName,
                CAST(0 AS BIT) AS RequiresMFA;
        END
    END
END
GO

-- 7. Procedure to get user roles
CREATE PROCEDURE [dbo].[usp_GetUserRoles]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Verify user exists
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @UserId)
    BEGIN
        RAISERROR('User not found', 16, 1);
        RETURN;
    END
    
    -- Get user roles
    SELECT 
        r.[Id] AS RoleId,
        r.[Name] AS RoleName,
        r.[Description] AS RoleDescription
    FROM 
        [dbo].[Roles] r
    INNER JOIN 
        [dbo].[UserRoles] ur ON r.Id = ur.RoleId
    WHERE 
        ur.UserId = @UserId;
END
GO

-- 8. Procedure to get all roles
CREATE PROCEDURE [dbo].[usp_GetAllRoles]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        [Id],
        [Name],
        [Description],
        [IsSystemRole]
    FROM 
        [dbo].[Roles]
    ORDER BY 
        [Name];
END
GO

-- 9. Procedure to get role by ID
CREATE PROCEDURE [dbo].[usp_GetRoleById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        [Id],
        [Name],
        [Description],
        [IsSystemRole]
    FROM 
        [dbo].[Roles]
    WHERE 
        [Id] = @Id;
        
    -- Get users in this role
    SELECT 
        u.[Id],
        u.[Username],
        u.[FirstName],
        u.[LastName]
    FROM 
        [dbo].[Users] u
    INNER JOIN 
        [dbo].[UserRoles] ur ON u.Id = ur.UserId
    WHERE 
        ur.RoleId = @Id
    ORDER BY 
        u.[Username];
END
GO

-- 10. Procedure to create a new role
CREATE PROCEDURE [dbo].[usp_CreateRole]
    @Name NVARCHAR(50),
    @Description NVARCHAR(200) = NULL,
    @IsSystemRole BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Check if role name already exists
        IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = @Name)
        BEGIN
            RAISERROR('Role name already exists', 16, 1);
            RETURN;
        END
        
        -- Insert the new role
        INSERT INTO [dbo].[Roles] (
            [Name],
            [Description],
            [IsSystemRole]
        )
        VALUES (
            @Name,
            @Description,
            @IsSystemRole
        );
        
        DECLARE @RoleId INT = SCOPE_IDENTITY();
        
        SELECT @RoleId AS RoleId, 'Role created successfully' AS Message;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 11. Procedure to update a role
CREATE PROCEDURE [dbo].[usp_UpdateRole]
    @Id INT,
    @Name NVARCHAR(50),
    @Description NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Check if role exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @Id)
        BEGIN
            RAISERROR('Role not found', 16, 1);
            RETURN;
        END
        
        -- Check if role name is taken by another role
        IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = @Name AND [Id] != @Id)
        BEGIN
            RAISERROR('Role name already taken by another role', 16, 1);
            RETURN;
        END
        
        -- Check if trying to modify a system role
        IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @Id AND [IsSystemRole] = 1)
        BEGIN
            RAISERROR('Cannot modify system roles', 16, 1);
            RETURN;
        END
        
        -- Update the role
        UPDATE [dbo].[Roles]
        SET 
            [Name] = @Name,
            [Description] = @Description,
            [UpdatedAt] = GETDATE()
        WHERE 
            [Id] = @Id;
            
        SELECT 'Role updated successfully' AS Message;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 12. Procedure to delete a role
CREATE PROCEDURE [dbo].[usp_DeleteRole]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if role exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @Id)
        BEGIN
            ROLLBACK;
            RAISERROR('Role not found', 16, 1);
            RETURN;
        END
        
        -- Check if trying to delete a system role
        IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @Id AND [IsSystemRole] = 1)
        BEGIN
            ROLLBACK;
            RAISERROR('Cannot delete system roles', 16, 1);
            RETURN;
        END
        
        -- Delete role assignments first
        DELETE FROM [dbo].[UserRoles]
        WHERE [RoleId] = @Id;
        
        -- Delete the role
        DELETE FROM [dbo].[Roles]
        WHERE [Id] = @Id;
        
        COMMIT;
        
        SELECT 'Role deleted successfully' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 13. Procedure to assign a role to a user
CREATE PROCEDURE [dbo].[usp_AssignRoleToUser]
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @UserId)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END
        
        -- Check if role exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @RoleId)
        BEGIN
            RAISERROR('Role not found', 16, 1);
            RETURN;
        END
        
        -- Check if assignment already exists
        IF EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = @UserId AND [RoleId] = @RoleId)
        BEGIN
            RAISERROR('User already has this role', 16, 1);
            RETURN;
        END
        
        -- Assign the role
        INSERT INTO [dbo].[UserRoles] (
            [UserId],
            [RoleId]
        )
        VALUES (
            @UserId,
            @RoleId
        );
        
        SELECT 'Role assigned successfully' AS Message;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- 14. Procedure to remove a role from a user
CREATE PROCEDURE [dbo].[usp_RemoveRoleFromUser]
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @UserId)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END
        
        -- Check if role exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @RoleId)
        BEGIN
            RAISERROR('Role not found', 16, 1);
            RETURN;
        END
        
        -- Check if assignment exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = @UserId AND [RoleId] = @RoleId)
        BEGIN
            RAISERROR('User does not have this role', 16, 1);
            RETURN;
        END
        
        -- Remove the role
        DELETE FROM [dbo].[UserRoles]
        WHERE [UserId] = @UserId AND [RoleId] = @RoleId;
        
        SELECT 'Role removed successfully' AS Message;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT 'All user management stored procedures created successfully.';
