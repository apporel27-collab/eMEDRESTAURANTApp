-- Update the sp_GetAllRecipes stored procedure
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllRecipes]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetAllRecipes]
GO

CREATE PROCEDURE [dbo].[sp_GetAllRecipes]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        r.Id,
        r.MenuItemId,
        m.Name AS MenuItemName,
        r.Title,
        r.PreparationInstructions,
        r.CookingInstructions,
        r.PlatingInstructions,
        r.Yield,
        r.YieldPercentage,
        r.PreparationTimeMinutes,
        r.CookingTimeMinutes,
        r.LastUpdated,
        r.Notes,
        r.IsArchived,
        r.Version
    FROM 
        Recipes r
        INNER JOIN MenuItems m ON r.MenuItemId = m.Id
    WHERE 
        r.IsArchived = 0
    ORDER BY 
        m.Name, r.Title
END
GO

PRINT 'The sp_GetAllRecipes stored procedure has been updated successfully.'
