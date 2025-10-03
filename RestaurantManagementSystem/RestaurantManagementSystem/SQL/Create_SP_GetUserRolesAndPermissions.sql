-- Create_SP_GetUserRolesAndPermissions.sql
-- Script to create the stored procedure for user authentication

-- Check if procedure exists and drop it if it does
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetUserRolesAndPermissions]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
GO

-- Create the stored procedure
CREATE PROCEDURE [dbo].[sp_GetUserRolesAndPermissions]
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get user ID
    DECLARE @UserId INT;
    SELECT @UserId = UserId FROM Users WHERE Username = @Username;
    
    -- Get user roles
    SELECT 
        r.RoleName
    FROM 
        UserRoles ur
    JOIN 
        Roles r ON ur.RoleId = r.RoleId
    WHERE 
        ur.UserId = @UserId;

END
GO
