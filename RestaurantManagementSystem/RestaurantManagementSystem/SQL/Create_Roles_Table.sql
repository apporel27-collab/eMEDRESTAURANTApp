-- Create_Roles_Table.sql
-- Script to create Roles table and ensure proper structure for authentication

-- Check if Roles table exists, create if not
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles](
        [RoleId] [int] IDENTITY(1,1) NOT NULL,
        [RoleName] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](255) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
    )

    -- Insert default roles
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Administrator', 'Full system access')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Manager', 'Restaurant manager access')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Staff', 'Regular staff access')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Kitchen', 'Kitchen staff access')
END
GO

-- Ensure UserRoles table exists with correct structure
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles](
        [UserRoleId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [RoleId] [int] NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserRoleId] ASC)
    )
    
    -- Add foreign key constraints
    ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Roles] 
    FOREIGN KEY([RoleId]) REFERENCES [dbo].[Roles] ([RoleId])
    
    ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
END
GO

-- Ensure the admin user has Administrator role
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    DECLARE @AdminUserId INT, @AdminRoleId INT
    
    SELECT @AdminUserId = UserId FROM Users WHERE Username = 'admin'
    SELECT @AdminRoleId = RoleId FROM Roles WHERE RoleName = 'Administrator'
    
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @AdminUserId AND RoleId = @AdminRoleId)
    BEGIN
        INSERT INTO UserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId)
    END
END
GO
