-- Adds FssaiNo column to dbo.RestaurantSettings if it does not exist
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[RestaurantSettings]') AND name = 'FssaiNo'
)
BEGIN
    ALTER TABLE [dbo].[RestaurantSettings]
    ADD [FssaiNo] NVARCHAR(32) NULL;
END
GO

-- Optional: update existing row with empty string to avoid nulls where needed
IF EXISTS (SELECT 1 FROM [dbo].[RestaurantSettings])
BEGIN
    UPDATE [dbo].[RestaurantSettings]
    SET [FssaiNo] = ISNULL([FssaiNo], '')
    WHERE [FssaiNo] IS NULL;
END
GO
