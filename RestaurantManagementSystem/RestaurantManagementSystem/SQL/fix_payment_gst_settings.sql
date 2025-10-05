-- Ensure dbo.RestaurantSettings table has DefaultGSTPercentage column and default value
USE [dev_Restaurant]
GO

-- Check if dbo.RestaurantSettings table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RestaurantSettings]') AND type in (N'U'))
BEGIN
    -- Create dbo.RestaurantSettings table if it doesn't exist
    CREATE TABLE [dbo].[RestaurantSettings](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RestaurantName] [nvarchar](100) NULL,
        [DefaultGSTPercentage] [decimal](5, 2) NULL DEFAULT (18.00),
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (getutcdate()),
        [UpdatedAt] [datetime2](7) NOT NULL DEFAULT (getutcdate()),
        CONSTRAINT [PK_RestaurantSettings] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
END

-- Check if DefaultGSTPercentage column exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RestaurantSettings]') AND name = 'DefaultGSTPercentage')
BEGIN
    ALTER TABLE [dbo].[RestaurantSettings] ADD [DefaultGSTPercentage] [decimal](5, 2) NULL DEFAULT (18.00)
END

-- Ensure we have at least one record with proper GST percentage
IF NOT EXISTS (SELECT 1 FROM [dbo].[RestaurantSettings])
BEGIN
    INSERT INTO [dbo].[RestaurantSettings] ([RestaurantName], [DefaultGSTPercentage], [CreatedAt], [UpdatedAt])
    VALUES ('My Restaurant', 18.00, GETUTCDATE(), GETUTCDATE())
END
ELSE
BEGIN
    -- Update existing record to have proper GST if it's null or 0
    UPDATE [dbo].[RestaurantSettings] 
    SET [DefaultGSTPercentage] = 18.00,
        [UpdatedAt] = GETUTCDATE()
    WHERE [DefaultGSTPercentage] IS NULL OR [DefaultGSTPercentage] = 0
END

-- Verify the settings
SELECT * FROM [dbo].[RestaurantSettings]