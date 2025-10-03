-- Direct_SQL_Fix.sql - Comprehensive SQL fix script
-- Created to fix column issues in the database

-- Create FullName computed column on Users table to avoid issues with u.FullName references
-- First check if the Users table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    -- Add FirstName and LastName columns if they don't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FirstName')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [FirstName] NVARCHAR(50) NULL;
        PRINT 'Added FirstName column to Users table.';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'LastName')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [LastName] NVARCHAR(50) NULL;
        PRINT 'Added LastName column to Users table.';
    END
    
    -- Set FirstName from Username if it's empty
    UPDATE [dbo].[Users]
    SET [FirstName] = Username
    WHERE ISNULL([FirstName], '') = '';
    
    -- Add computed column for FullName if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FullName')
    BEGIN
        ALTER TABLE [dbo].[Users] 
        ADD [FullName] AS (LTRIM(RTRIM(ISNULL([FirstName], '') + ' ' + ISNULL([LastName], ''))));
        PRINT 'Added FullName computed column to Users table.';
    END
    
    -- Add Phone column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Phone')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(20) NULL;
        PRINT 'Added Phone column to Users table.';
    END
    
    -- Add Role column if it doesn't exist (with default value 3)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Role')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [Role] INT NOT NULL DEFAULT 3;
        PRINT 'Added Role column to Users table.';
    END

    -- Update Role from RoleId if both exist
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Role')
    AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'RoleId')
    BEGIN
        UPDATE [dbo].[Users] SET [Role] = [RoleId];
        PRINT 'Updated Role column with RoleId values.';
    END
END
ELSE
BEGIN
    -- Create Users table if it doesn't exist
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL,
        [Password] NVARCHAR(100) NOT NULL,
        [FirstName] NVARCHAR(50) NOT NULL DEFAULT '',
        [LastName] NVARCHAR(50) NULL,
        [Email] NVARCHAR(100) NULL,
        [Phone] NVARCHAR(20) NULL,
        [Role] INT NOT NULL DEFAULT 3,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [FullName] AS (LTRIM(RTRIM(ISNULL([FirstName], '') + ' ' + ISNULL([LastName], ''))))
    );
    PRINT 'Created Users table with all required columns.';
    
    -- Insert default admin user if no users exist
    INSERT INTO [dbo].[Users] (Username, Password, FirstName, LastName, Email, Role, IsActive)
    VALUES ('admin', 'Admin@123', 'System', 'Administrator', 'admin@restaurant.com', 1, 1);
    PRINT 'Added default admin user.';
END
