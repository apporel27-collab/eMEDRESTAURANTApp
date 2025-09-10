-- Drop and recreate table columns first if they don't exist
BEGIN TRY
    -- Check if PLUCode column exists and add if not
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'PLUCode')
    BEGIN
        ALTER TABLE dbo.MenuItems ADD PLUCode NVARCHAR(20) NULL DEFAULT '';
        PRINT 'Added PLUCode column';
    END
    
    -- Check if CalorieCount column exists and add if not
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'CalorieCount')
    BEGIN
        ALTER TABLE dbo.MenuItems ADD CalorieCount INT NULL;
        PRINT 'Added CalorieCount column';
    END
    
    -- Check if IsFeatured column exists and add if not
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'IsFeatured')
    BEGIN
        ALTER TABLE dbo.MenuItems ADD IsFeatured BIT NOT NULL DEFAULT 0;
        PRINT 'Added IsFeatured column';
    END
    
    -- Check if IsSpecial column exists and add if not
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'IsSpecial')
    BEGIN
        ALTER TABLE dbo.MenuItems ADD IsSpecial BIT NOT NULL DEFAULT 0;
        PRINT 'Added IsSpecial column';
    END
    
    -- Check if DiscountPercentage column exists and add if not
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'DiscountPercentage')
    BEGIN
        ALTER TABLE dbo.MenuItems ADD DiscountPercentage DECIMAL(5, 2) NULL;
        PRINT 'Added DiscountPercentage column';
    END
    
    -- Check if TargetGP column exists and add if not
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'TargetGP')
    BEGIN
        ALTER TABLE dbo.MenuItems ADD TargetGP DECIMAL(5, 2) NULL;
        PRINT 'Added TargetGP column';
    END
    
    -- Check if PreparationTimeMinutes column exists (PrepTime might be the actual name)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'PreparationTimeMinutes')
    BEGIN
        -- Let's create a mapping between PrepTime and PreparationTimeMinutes
        PRINT 'Using PrepTime as PreparationTimeMinutes in stored procedure';
    END
END TRY
BEGIN CATCH
    PRINT 'Error updating table schema: ' + ERROR_MESSAGE();
END CATCH

-- Now drop and recreate the stored procedure
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllMenuItems]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetAllMenuItems]
GO

CREATE PROCEDURE [dbo].[sp_GetAllMenuItems]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        m.[Id], 
        ISNULL(m.[PLUCode], '') AS PLUCode, -- Handle NULL if column was just added
        m.[Name], 
        m.[Description], 
        m.[Price], 
        m.[CategoryId], 
        c.[Name] AS CategoryName,
        m.[ImagePath], 
        m.[IsAvailable], 
        -- Map PrepTime to PreparationTimeMinutes
        m.[PrepTime] AS PreparationTimeMinutes,
        m.[CalorieCount],
        ISNULL(m.[IsFeatured], 0) AS IsFeatured, -- Handle NULL if column was just added
        ISNULL(m.[IsSpecial], 0) AS IsSpecial, -- Handle NULL if column was just added
        m.[DiscountPercentage],
        m.[TargetGP]
    FROM [dbo].[MenuItems] m
    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
    ORDER BY m.[Name];
END
GO
