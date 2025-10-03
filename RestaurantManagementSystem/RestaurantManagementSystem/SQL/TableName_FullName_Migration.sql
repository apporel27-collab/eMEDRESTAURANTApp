-- Add TableName column to Tables table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tables') AND name = 'TableName')
BEGIN
    ALTER TABLE [dbo].[Tables]
    ADD [TableName] AS ([TableNumber]);  -- Create a computed column that mirrors TableNumber
END;

-- Add FullName column to Users table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FullName')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [FullName] AS (LTRIM(RTRIM(ISNULL([FirstName], '') + ' ' + ISNULL([LastName], ''))));  -- Create a computed column that combines FirstName and LastName
END;
