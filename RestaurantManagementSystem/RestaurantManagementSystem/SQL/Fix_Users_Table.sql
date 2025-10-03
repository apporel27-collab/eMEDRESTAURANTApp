-- Fix_Users_Table.sql
-- Run this script to fix the Users table structure

-- Check if Users table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    PRINT 'Updating Users table structure...';
    
    -- Add Phone column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Phone')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(20) NULL;
        PRINT 'Added Phone column to Users table.';
    END
    
    -- Add Role column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Role')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [Role] INT NOT NULL DEFAULT 3;
        PRINT 'Added Role column to Users table.';
    END
    
    -- Update Role column with RoleId values if needed
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'RoleId')
    BEGIN
        UPDATE [dbo].[Users] SET [Role] = [RoleId] WHERE [Role] <> [RoleId];
        PRINT 'Updated Role column with RoleId values.';
    END
    
    -- Add FullName computed column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FullName')
    BEGIN
        ALTER TABLE [dbo].[Users] 
        ADD [FullName] AS (LTRIM(RTRIM(ISNULL([FirstName], '') + ' ' + ISNULL([LastName], ''))));
        PRINT 'Added FullName computed column to Users table.';
    END
    
    PRINT 'Users table structure update complete.';
END
ELSE
BEGIN
    PRINT 'Users table does not exist. No updates performed.';
END
