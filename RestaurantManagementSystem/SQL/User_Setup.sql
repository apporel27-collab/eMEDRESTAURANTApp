-- Create Users Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL,
        [Password] NVARCHAR(100) NOT NULL,  -- In production, should be hashed
        [FirstName] NVARCHAR(50) NOT NULL,
        [LastName] NVARCHAR(50) NULL,
        [Email] NVARCHAR(100) NULL,
        [Phone] NVARCHAR(20) NULL,
        [Role] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [LastLogin] DATETIME NULL
    );

    -- Add a default admin user
    INSERT INTO [dbo].[Users] 
        ([Username], [Password], [FirstName], [LastName], [Email], [Role], [IsActive])
    VALUES
        ('admin', 'admin123', 'System', 'Administrator', 'admin@restaurant.com', 9, 1);  -- 9 = RestaurantManager
END
GO

-- Create stored procedure for user management
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpsertUser')
    DROP PROCEDURE usp_UpsertUser
GO

CREATE PROCEDURE [dbo].[usp_UpsertUser]
    @Id INT,
    @Username NVARCHAR(50),
    @Password NVARCHAR(100) = NULL,
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @Role INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Message NVARCHAR(200);

    -- Check if username exists for a different user (for new or updates)
    IF EXISTS (SELECT 1 FROM [Users] WHERE [Username] = @Username AND [Id] <> @Id)
    BEGIN
        SET @Message = 'Username already exists. Please choose another username.';
        SELECT @Message AS [Message];
        RETURN;
    END
    
    -- INSERT or UPDATE based on Id
    IF @Id = 0
    BEGIN
        -- Insert new user
        INSERT INTO [Users] (
            [Username], 
            [Password], 
            [FirstName], 
            [LastName], 
            [Email], 
            [Phone], 
            [Role], 
            [IsActive]
        )
        VALUES (
            @Username, 
            @Password, 
            @FirstName, 
            @LastName, 
            @Email, 
            @Phone, 
            @Role, 
            @IsActive
        );
        
        SET @Message = 'User created successfully.';
    END
    ELSE
    BEGIN
        -- Update existing user
        IF @Password IS NOT NULL 
        BEGIN
            -- Update with password change
            UPDATE [Users]
            SET 
                [Username] = @Username,
                [Password] = @Password,
                [FirstName] = @FirstName,
                [LastName] = @LastName,
                [Email] = @Email,
                [Phone] = @Phone,
                [Role] = @Role,
                [IsActive] = @IsActive
            WHERE [Id] = @Id;
        END
        ELSE
        BEGIN
            -- Update without password change
            UPDATE [Users]
            SET 
                [Username] = @Username,
                [FirstName] = @FirstName,
                [LastName] = @LastName,
                [Email] = @Email,
                [Phone] = @Phone,
                [Role] = @Role,
                [IsActive] = @IsActive
            WHERE [Id] = @Id;
        END
        
        SET @Message = 'User updated successfully.';
    END
    
    SELECT @Message AS [Message];
END
GO
