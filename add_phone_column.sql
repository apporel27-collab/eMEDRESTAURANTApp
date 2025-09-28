-- Script to add Phone column to Users table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Phone')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(20) NULL;
    PRINT 'Added Phone column to Users table.';
END
