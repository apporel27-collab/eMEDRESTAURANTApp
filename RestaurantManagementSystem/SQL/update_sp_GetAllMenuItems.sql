-- Update sp_GetAllMenuItems to include KitchenStationId
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllMenuItems]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetAllMenuItems]
GO

CREATE PROCEDURE [dbo].[sp_GetAllMenuItems]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        m.[Id], 
        ISNULL(m.[PLUCode], '') AS PLUCode,
        m.[Name], 
        m.[Description], 
        m.[Price], 
        m.[CategoryId], 
        c.[Name] AS CategoryName,
        m.[ImagePath], 
        m.[IsAvailable], 
        m.[PrepTime] AS PreparationTimeMinutes,
        m.[CalorieCount],
        ISNULL(m.[IsFeatured], 0) AS IsFeatured,
        ISNULL(m.[IsSpecial], 0) AS IsSpecial,
        m.[DiscountPercentage],
        m.[KitchenStationId], -- Added this column
        m.[TargetGP]
    FROM [dbo].[MenuItems] m
    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
    ORDER BY m.[Name];
END
GO
