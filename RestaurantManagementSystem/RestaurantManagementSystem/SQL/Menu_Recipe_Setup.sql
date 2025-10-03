-- Menu and Recipe Management SQL Setup Script
-- Created: September 1, 2025

-- Create Allergens table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Allergens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Allergens] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [IconPath] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    
    -- Insert common allergens
    INSERT INTO [dbo].[Allergens] ([Name], [Description], [IsActive])
    VALUES 
        ('Gluten', 'Contains wheat, barley, rye, or other gluten-containing grains', 1),
        ('Dairy', 'Contains milk products including lactose', 1),
        ('Nuts', 'Contains tree nuts such as almonds, walnuts, pecans', 1),
        ('Peanuts', 'Contains peanuts or peanut derivatives', 1),
        ('Shellfish', 'Contains shellfish like shrimp, crab, lobster', 1),
        ('Fish', 'Contains fish or fish derivatives', 1),
        ('Eggs', 'Contains eggs or egg derivatives', 1),
        ('Soy', 'Contains soybeans or soybean derivatives', 1),
        ('Sesame', 'Contains sesame seeds or sesame derivatives', 1);
    
    PRINT 'Created Allergens table and inserted common allergens';
END
ELSE
    PRINT 'Allergens table already exists';

-- Create Modifiers table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Modifiers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Modifiers] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(255) NULL,
        [ModifierType] NVARCHAR(50) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    
    -- Insert common modifiers
    INSERT INTO [dbo].[Modifiers] ([Name], [Description], [ModifierType], [IsActive])
    VALUES 
        ('Extra Cheese', 'Add additional cheese to the dish', 'Addition', 1),
        ('No Onions', 'Remove onions from the dish', 'Removal', 1),
        ('Substitute Fries for Salad', 'Replace fries with salad', 'Substitution', 1),
        ('Extra Spicy', 'Make the dish spicier than standard', 'Addition', 1),
        ('Gluten-Free Bun', 'Use gluten-free bun instead of regular', 'Substitution', 1);
    
    PRINT 'Created Modifiers table and inserted common modifiers';
END
ELSE
    PRINT 'Modifiers table already exists';

-- Create MenuItems table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MenuItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MenuItems] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PLUCode] NVARCHAR(20) NOT NULL UNIQUE,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [Price] DECIMAL(10,2) NOT NULL,
        [CategoryId] INT NOT NULL,
        [ImagePath] NVARCHAR(200) NULL,
        [IsAvailable] BIT NOT NULL DEFAULT 1,
        [PreparationTimeMinutes] INT NOT NULL DEFAULT 15,
        [CalorieCount] INT NULL,
        [IsFeatured] BIT NOT NULL DEFAULT 0,
        [IsSpecial] BIT NOT NULL DEFAULT 0,
        [DiscountPercentage] DECIMAL(5,2) NULL,
        [KitchenStationId] INT NULL,
        CONSTRAINT [FK_MenuItems_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id])
    );
    
    PRINT 'Created MenuItems table';
END
ELSE
    PRINT 'MenuItems table already exists';
   

-- Create MenuItemAllergens table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MenuItemAllergens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MenuItemAllergens] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [AllergenId] INT NOT NULL,
        [SeverityLevel] INT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_MenuItemAllergens_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MenuItemAllergens_Allergens] FOREIGN KEY ([AllergenId]) REFERENCES [dbo].[Allergens] ([Id])
    );
    
    PRINT 'Created MenuItemAllergens table';
END
ELSE
    PRINT 'MenuItemAllergens table already exists';

-- Create MenuItemModifiers table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MenuItemModifiers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MenuItemModifiers] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [ModifierId] INT NOT NULL,
        [PriceAdjustment] DECIMAL(10,2) NOT NULL DEFAULT 0.00,
        [IsDefault] BIT NOT NULL DEFAULT 0,
        [MaxAllowed] INT NULL,
        CONSTRAINT [FK_MenuItemModifiers_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MenuItemModifiers_Modifiers] FOREIGN KEY ([ModifierId]) REFERENCES [dbo].[Modifiers] ([Id])
    );
    
    PRINT 'Created MenuItemModifiers table';
END
ELSE
    PRINT 'MenuItemModifiers table already exists';

  

-- Create MenuItemIngredients table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MenuItemIngredients]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MenuItemIngredients] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [IngredientId] INT NOT NULL,
        [Quantity] DECIMAL(10,2) NOT NULL,
        [Unit] NVARCHAR(20) NOT NULL,
        [IsOptional] BIT NOT NULL DEFAULT 0,
        [Instructions] NVARCHAR(200) NULL,
        CONSTRAINT [FK_MenuItemIngredients_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MenuItemIngredients_Ingredients] FOREIGN KEY ([IngredientId]) REFERENCES [dbo].[Ingredients] ([Id])
    );
    
    PRINT 'Created MenuItemIngredients table';
END
ELSE
    PRINT 'MenuItemIngredients table already exists';

-- Create Recipes table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Recipes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Recipes] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [Title] NVARCHAR(100) NOT NULL,
        [PreparationInstructions] NVARCHAR(MAX) NOT NULL,
        [CookingInstructions] NVARCHAR(MAX) NOT NULL,
        [PlatingInstructions] NVARCHAR(MAX) NULL,
        [Yield] INT NOT NULL DEFAULT 1,
        [PreparationTimeMinutes] INT NOT NULL,
        [CookingTimeMinutes] INT NOT NULL,
        [LastUpdated] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedById] INT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [Version] INT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_Recipes_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id]) ON DELETE CASCADE
    );
    
    PRINT 'Created Recipes table';
END
ELSE
    PRINT 'Recipes table already exists';

-- Create RecipeSteps table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RecipeSteps]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RecipeSteps] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RecipeId] INT NOT NULL,
        [StepNumber] INT NOT NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        [TimeRequiredMinutes] INT NULL,
        [Temperature] NVARCHAR(50) NULL,
        [SpecialEquipment] NVARCHAR(100) NULL,
        [Tips] NVARCHAR(MAX) NULL,
        [ImagePath] NVARCHAR(200) NULL,
        CONSTRAINT [FK_RecipeSteps_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]) ON DELETE CASCADE
    );
    
    -- Create index for ordered steps
    CREATE INDEX [IX_RecipeSteps_RecipeId_StepNumber] ON [dbo].[RecipeSteps] ([RecipeId], [StepNumber]);
    
    PRINT 'Created RecipeSteps table';
END
ELSE
    PRINT 'RecipeSteps table already exists';

-- Create MenuVersionHistory table for version control (BR-020)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MenuVersionHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MenuVersionHistory] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [ChangeType] NVARCHAR(20) NOT NULL, -- Create, Update, Delete
        [ChangeDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ChangedByUserId] INT NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [ApprovalStatus] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
        [ApprovalDate] DATETIME NULL,
        [ApprovedByUserId] INT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_MenuVersionHistory_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id])
    );
    
    PRINT 'Created MenuVersionHistory table';
END
ELSE
    PRINT 'MenuVersionHistory table already exists';

-- Create PriceChangeApproval table to track price change approvals
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PriceChangeApproval]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PriceChangeApproval] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [OldPrice] DECIMAL(10,2) NOT NULL,
        [NewPrice] DECIMAL(10,2) NOT NULL,
        [RequestDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [RequestedByUserId] INT NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
        [ApprovalDate] DATETIME NULL,
        [ApprovedByUserId] INT NULL,
        [Reason] NVARCHAR(MAX) NOT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_PriceChangeApproval_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id])
    );
    
    PRINT 'Created PriceChangeApproval table';
END
ELSE
    PRINT 'PriceChangeApproval table already exists';

-- Add TargetGP column to MenuItems table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MenuItems]') AND name = 'TargetGP')
BEGIN
    ALTER TABLE [dbo].[MenuItems]
    ADD [TargetGP] DECIMAL(5,2) NULL;
    
    PRINT 'Added TargetGP column to MenuItems table';
END

-- Add LastPurchaseCost column to Ingredients table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Ingredients]') AND name = 'LastPurchaseCost')
BEGIN
    ALTER TABLE [dbo].[Ingredients]
    ADD [LastPurchaseCost] DECIMAL(10,2) NULL DEFAULT 0.00,
        [LastPurchaseDate] DATETIME NULL,
        [UnitOfMeasure] NVARCHAR(20) NULL;
    
    PRINT 'Added cost tracking columns to Ingredients table';
END

-- Add YieldPercentage column to Recipes table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Recipes]') AND name = 'YieldPercentage')
BEGIN
    ALTER TABLE [dbo].[Recipes]
    ADD [YieldPercentage] DECIMAL(5,2) NULL DEFAULT 100.00;
    
    PRINT 'Added YieldPercentage column to Recipes table';
END

-- Create POSPublishStatus table for tracking published menu items
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[POSPublishStatus]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[POSPublishStatus] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuItemId] INT NOT NULL,
        [IsPublishedToPOS] BIT NOT NULL DEFAULT 0,
        [IsPublishedToOnline] BIT NOT NULL DEFAULT 0,
        [LastPublishedToPOS] DATETIME NULL,
        [LastPublishedToOnline] DATETIME NULL,
        [PublishedByUserId] INT NULL,
        CONSTRAINT [FK_POSPublishStatus_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id])
    );
    
    PRINT 'Created POSPublishStatus table';
END
ELSE
    PRINT 'POSPublishStatus table already exists';

-- =============================================
-- STORED PROCEDURES
-- =============================================

-- Create stored procedure to get all menu items
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllMenuItems]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetAllMenuItems]
GO

CREATE PROCEDURE [dbo].[sp_GetAllMenuItems]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        m.[Id], 
        m.[PLUCode], 
        m.[Name], 
        m.[Description], 
        m.[Price], 
        m.[CategoryId], 
        c.[Name] AS CategoryName,
        m.[ImagePath], 
        m.[IsAvailable], 
        m.[PreparationTimeMinutes], 
        m.[CalorieCount], 
        m.[IsFeatured], 
        m.[IsSpecial], 
        m.[DiscountPercentage],
        m.[TargetGP]
    FROM [dbo].[MenuItems] m
    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
    ORDER BY m.[Name];
END
GO

-- Create stored procedure to get menu item by ID
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetMenuItemById]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetMenuItemById]
GO

CREATE PROCEDURE [dbo].[sp_GetMenuItemById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        m.[Id], 
        m.[PLUCode], 
        m.[Name], 
        m.[Description], 
        m.[Price], 
        m.[CategoryId], 
        c.[Name] AS CategoryName,
        m.[ImagePath], 
        m.[IsAvailable], 
        m.[PreparationTimeMinutes], 
        m.[CalorieCount], 
        m.[IsFeatured], 
        m.[IsSpecial], 
        m.[DiscountPercentage],
        m.[TargetGP]
    FROM [dbo].[MenuItems] m
    INNER JOIN [dbo].[Categories] c ON m.[CategoryId] = c.[Id]
    WHERE m.[Id] = @Id;
END
GO

-- Create stored procedure to create a new menu item with versioning
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateMenuItem]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_CreateMenuItem]
GO

CREATE PROCEDURE [dbo].[sp_CreateMenuItem]
    @PLUCode NVARCHAR(20),
    @Name NVARCHAR(100),
    @Description NVARCHAR(500),
    @Price DECIMAL(10,2),
    @CategoryId INT,
    @ImagePath NVARCHAR(200) = NULL,
    @IsAvailable BIT = 1,
    @PreparationTimeMinutes INT = 15,
    @CalorieCount INT = NULL,
    @IsFeatured BIT = 0,
    @IsSpecial BIT = 0,
    @DiscountPercentage DECIMAL(5,2) = NULL,
    @KitchenStationId INT = NULL,
    @TargetGP DECIMAL(5,2) = NULL,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NewMenuItemId INT;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Insert the new menu item
        INSERT INTO [dbo].[MenuItems] (
            [PLUCode], [Name], [Description], [Price], [CategoryId], 
            [ImagePath], [IsAvailable], [PreparationTimeMinutes], [CalorieCount], 
            [IsFeatured], [IsSpecial], [DiscountPercentage], [KitchenStationId], [TargetGP]
        )
        VALUES (
            @PLUCode, @Name, @Description, @Price, @CategoryId,
            @ImagePath, @IsAvailable, @PreparationTimeMinutes, @CalorieCount,
            @IsFeatured, @IsSpecial, @DiscountPercentage, @KitchenStationId, @TargetGP
        );
        
        SET @NewMenuItemId = SCOPE_IDENTITY();
        
        -- Log the creation in version history
        INSERT INTO [dbo].[MenuVersionHistory] (
            [MenuItemId], [ChangeType], [ChangeDate], [ChangedByUserId],
            [NewValue], [ApprovalStatus]
        )
        VALUES (
            @NewMenuItemId, 'Create', GETDATE(), @UserId,
            (SELECT * FROM [dbo].[MenuItems] WHERE [Id] = @NewMenuItemId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            'Approved' -- Auto-approve creation
        );
        
        -- Create entry in POSPublishStatus table
        INSERT INTO [dbo].[POSPublishStatus] (
            [MenuItemId], [IsPublishedToPOS], [IsPublishedToOnline]
        )
        VALUES (
            @NewMenuItemId, 0, 0
        );
        
        COMMIT TRANSACTION;
        
        -- Return the new menu item ID
        SELECT @NewMenuItemId AS NewMenuItemId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to update an existing menu item with versioning
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateMenuItem]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_UpdateMenuItem]
GO

CREATE PROCEDURE [dbo].[sp_UpdateMenuItem]
    @Id INT,
    @PLUCode NVARCHAR(20),
    @Name NVARCHAR(100),
    @Description NVARCHAR(500),
    @Price DECIMAL(10,2),
    @CategoryId INT,
    @ImagePath NVARCHAR(200) = NULL,
    @IsAvailable BIT = 1,
    @PreparationTimeMinutes INT = 15,
    @CalorieCount INT = NULL,
    @IsFeatured BIT = 0,
    @IsSpecial BIT = 0,
    @DiscountPercentage DECIMAL(5,2) = NULL,
    @KitchenStationId INT = NULL,
    @TargetGP DECIMAL(5,2) = NULL,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OldPrice DECIMAL(10,2);
    DECLARE @OldData NVARCHAR(MAX);
    DECLARE @NeedsPriceApproval BIT = 0;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Get the old data for versioning
        SELECT @OldPrice = [Price], @OldData = (SELECT * FROM [dbo].[MenuItems] WHERE [Id] = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        FROM [dbo].[MenuItems]
        WHERE [Id] = @Id;
        
        -- Check if price has changed and requires approval
        IF @OldPrice <> @Price
        BEGIN
            SET @NeedsPriceApproval = 1;
            
            -- Create price change approval request
            INSERT INTO [dbo].[PriceChangeApproval] (
                [MenuItemId], [OldPrice], [NewPrice], [RequestDate], 
                [RequestedByUserId], [Status], [Reason]
            )
            VALUES (
                @Id, @OldPrice, @Price, GETDATE(), 
                @UserId, 'Pending', 'Price update from ' + CAST(@OldPrice AS NVARCHAR) + ' to ' + CAST(@Price AS NVARCHAR)
            );
            
            -- Keep the old price until approved
            SET @Price = @OldPrice;
        END
        
        -- Update the menu item
        UPDATE [dbo].[MenuItems]
        SET 
            [PLUCode] = @PLUCode,
            [Name] = @Name,
            [Description] = @Description,
            [Price] = @Price, -- This will be the old price if price change needs approval
            [CategoryId] = @CategoryId,
            [ImagePath] = @ImagePath,
            [IsAvailable] = @IsAvailable,
            [PreparationTimeMinutes] = @PreparationTimeMinutes,
            [CalorieCount] = @CalorieCount,
            [IsFeatured] = @IsFeatured,
            [IsSpecial] = @IsSpecial,
            [DiscountPercentage] = @DiscountPercentage,
            [KitchenStationId] = @KitchenStationId,
            [TargetGP] = @TargetGP
        WHERE [Id] = @Id;
        
        -- Log the update in version history
        INSERT INTO [dbo].[MenuVersionHistory] (
            [MenuItemId], [ChangeType], [ChangeDate], [ChangedByUserId],
            [OldValue], [NewValue], [ApprovalStatus], [Notes]
        )
        VALUES (
            @Id, 'Update', GETDATE(), @UserId,
            @OldData,
            (SELECT * FROM [dbo].[MenuItems] WHERE [Id] = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            CASE WHEN @NeedsPriceApproval = 1 THEN 'Pending' ELSE 'Approved' END,
            CASE WHEN @NeedsPriceApproval = 1 THEN 'Price change requires approval' ELSE NULL END
        );
        
        COMMIT TRANSACTION;
        
        -- Return status indicating if price approval is needed
        SELECT @NeedsPriceApproval AS NeedsPriceApproval;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure for approving price changes
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ApprovePriceChange]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_ApprovePriceChange]
GO

CREATE PROCEDURE [dbo].[sp_ApprovePriceChange]
    @PriceChangeId INT,
    @Approved BIT,
    @ApprovedByUserId INT,
    @Notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @MenuItemId INT;
    DECLARE @NewPrice DECIMAL(10,2);
    DECLARE @ApprovalStatus NVARCHAR(20);
    
    SET @ApprovalStatus = CASE WHEN @Approved = 1 THEN 'Approved' ELSE 'Rejected' END;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Get the menu item ID and new price
        SELECT @MenuItemId = [MenuItemId], @NewPrice = [NewPrice]
        FROM [dbo].[PriceChangeApproval]
        WHERE [Id] = @PriceChangeId;
        
        -- Update the approval record
        UPDATE [dbo].[PriceChangeApproval]
        SET 
            [Status] = @ApprovalStatus,
            [ApprovalDate] = GETDATE(),
            [ApprovedByUserId] = @ApprovedByUserId,
            [Notes] = @Notes
        WHERE [Id] = @PriceChangeId;
        
        -- If approved, update the menu item price
        IF @Approved = 1
        BEGIN
            UPDATE [dbo].[MenuItems]
            SET [Price] = @NewPrice
            WHERE [Id] = @MenuItemId;
        END
        
        -- Update the version history approval status
        UPDATE [dbo].[MenuVersionHistory]
        SET 
            [ApprovalStatus] = @ApprovalStatus,
            [ApprovalDate] = GETDATE(),
            [ApprovedByUserId] = @ApprovedByUserId,
            [Notes] = @Notes
        WHERE [MenuItemId] = @MenuItemId
          AND [ApprovalStatus] = 'Pending'
          AND [ChangeType] = 'Update'
          AND [ChangeDate] = (
              SELECT MAX([ChangeDate]) 
              FROM [dbo].[MenuVersionHistory] 
              WHERE [MenuItemId] = @MenuItemId AND [ApprovalStatus] = 'Pending'
          );
        
        COMMIT TRANSACTION;
        
        -- Return success status
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to calculate suggested price based on target GP%
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CalculateSuggestedPrice]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_CalculateSuggestedPrice]
GO

CREATE PROCEDURE [dbo].[sp_CalculateSuggestedPrice]
    @MenuItemId INT,
    @TargetGPPercentage DECIMAL(5,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TotalCost DECIMAL(10,2) = 0;
    DECLARE @SuggestedPrice DECIMAL(10,2);
    
    -- Calculate the total cost from ingredients
    SELECT @TotalCost = SUM(i.[LastPurchaseCost] * mi.[Quantity])
    FROM [dbo].[MenuItemIngredients] mi
    INNER JOIN [dbo].[Ingredients] i ON mi.[IngredientId] = i.[Id]
    WHERE mi.[MenuItemId] = @MenuItemId;
    
    -- If we have a recipe with yield percentage, adjust for yield
    DECLARE @YieldPercentage DECIMAL(5,2);
    SELECT @YieldPercentage = [YieldPercentage]
    FROM [dbo].[Recipes]
    WHERE [MenuItemId] = @MenuItemId;
    
    -- Adjust cost based on yield if available
    IF @YieldPercentage IS NOT NULL AND @YieldPercentage > 0
    BEGIN
        SET @TotalCost = @TotalCost * (100.0 / @YieldPercentage);
    END
    
    -- Calculate suggested price based on GP%
    -- Formula: Price = Cost / (1 - GP%)
    IF @TotalCost > 0
    BEGIN
        SET @SuggestedPrice = @TotalCost / (1 - (@TargetGPPercentage / 100.0));
        
        -- Round to nearest ₹0.49 or ₹0.99 for pricing psychology
        SET @SuggestedPrice = ROUND(@SuggestedPrice * 2, 0) / 2 - 0.01;
    END
    ELSE
    BEGIN
        SET @SuggestedPrice = 0;
    END
    
    -- Return the suggested price
    SELECT 
        @MenuItemId AS MenuItemId, 
        @TotalCost AS TotalCost, 
        @TargetGPPercentage AS TargetGPPercentage,
        @SuggestedPrice AS SuggestedPrice;
END
GO

-- Create stored procedure to publish menu item to POS/Online
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_PublishMenuItem]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_PublishMenuItem]
GO

CREATE PROCEDURE [dbo].[sp_PublishMenuItem]
    @MenuItemId INT,
    @PublishToPOS BIT,
    @PublishToOnline BIT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Check if the menu item exists
        IF NOT EXISTS (SELECT 1 FROM [dbo].[MenuItems] WHERE [Id] = @MenuItemId)
        BEGIN
            THROW 50000, 'Menu item does not exist', 1;
        END
        
        -- Check if the menu item is already published
        IF NOT EXISTS (SELECT 1 FROM [dbo].[POSPublishStatus] WHERE [MenuItemId] = @MenuItemId)
        BEGIN
            -- Create a new publish status record
            INSERT INTO [dbo].[POSPublishStatus] (
                [MenuItemId], 
                [IsPublishedToPOS], 
                [IsPublishedToOnline],
                [LastPublishedToPOS], 
                [LastPublishedToOnline],
                [PublishedByUserId]
            )
            VALUES (
                @MenuItemId, 
                @PublishToPOS, 
                @PublishToOnline,
                CASE WHEN @PublishToPOS = 1 THEN GETDATE() ELSE NULL END,
                CASE WHEN @PublishToOnline = 1 THEN GETDATE() ELSE NULL END,
                @UserId
            );
        END
        ELSE
        BEGIN
            -- Update existing publish status
            UPDATE [dbo].[POSPublishStatus]
            SET 
                [IsPublishedToPOS] = @PublishToPOS,
                [IsPublishedToOnline] = @PublishToOnline,
                [LastPublishedToPOS] = CASE 
                                          WHEN @PublishToPOS = 1 AND ([IsPublishedToPOS] = 0 OR [IsPublishedToPOS] IS NULL) 
                                          THEN GETDATE() 
                                          WHEN @PublishToPOS = 1 AND [IsPublishedToPOS] = 1 
                                          THEN [LastPublishedToPOS]
                                          ELSE NULL 
                                       END,
                [LastPublishedToOnline] = CASE 
                                            WHEN @PublishToOnline = 1 AND ([IsPublishedToOnline] = 0 OR [IsPublishedToOnline] IS NULL) 
                                            THEN GETDATE() 
                                            WHEN @PublishToOnline = 1 AND [IsPublishedToOnline] = 1 
                                            THEN [LastPublishedToOnline]
                                            ELSE NULL 
                                          END,
                [PublishedByUserId] = @UserId
            WHERE [MenuItemId] = @MenuItemId;
        END
        
        -- Log the publish action in version history
        INSERT INTO [dbo].[MenuVersionHistory] (
            [MenuItemId], 
            [ChangeType], 
            [ChangeDate], 
            [ChangedByUserId],
            [NewValue], 
            [ApprovalStatus],
            [Notes]
        )
        VALUES (
            @MenuItemId, 
            'Publish', 
            GETDATE(), 
            @UserId,
            (SELECT * FROM [dbo].[POSPublishStatus] WHERE [MenuItemId] = @MenuItemId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            'Approved',
            CASE 
                WHEN @PublishToPOS = 1 AND @PublishToOnline = 1 THEN 'Published to both POS and Online'
                WHEN @PublishToPOS = 1 THEN 'Published to POS only'
                WHEN @PublishToOnline = 1 THEN 'Published to Online only'
                ELSE 'Unpublished from all platforms'
            END
        );
        
        COMMIT TRANSACTION;
        
        -- Return success status
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to manage recipe for a menu item
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ManageRecipe]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_ManageRecipe]
GO

CREATE PROCEDURE [dbo].[sp_ManageRecipe]
    @MenuItemId INT,
    @Title NVARCHAR(100),
    @PreparationInstructions NVARCHAR(MAX),
    @CookingInstructions NVARCHAR(MAX),
    @PlatingInstructions NVARCHAR(MAX) = NULL,
    @Yield INT = 1,
    @YieldPercentage DECIMAL(5,2) = 100.00,
    @PreparationTimeMinutes INT,
    @CookingTimeMinutes INT,
    @Notes NVARCHAR(MAX) = NULL,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RecipeId INT;
    DECLARE @IsUpdate BIT = 0;
    DECLARE @CurrentVersion INT = 1;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Check if a recipe already exists for this menu item
        SELECT @RecipeId = [Id], @CurrentVersion = [Version]
        FROM [dbo].[Recipes]
        WHERE [MenuItemId] = @MenuItemId AND [IsArchived] = 0;
        
        IF @RecipeId IS NOT NULL
        BEGIN
            SET @IsUpdate = 1;
            SET @CurrentVersion = @CurrentVersion + 1;
            
            -- Archive the current recipe
            UPDATE [dbo].[Recipes]
            SET [IsArchived] = 1
            WHERE [Id] = @RecipeId;
        END
        
        -- Create a new recipe version
        INSERT INTO [dbo].[Recipes] (
            [MenuItemId], 
            [Title], 
            [PreparationInstructions], 
            [CookingInstructions], 
            [PlatingInstructions], 
            [Yield], 
            [YieldPercentage],
            [PreparationTimeMinutes], 
            [CookingTimeMinutes], 
            [LastUpdated], 
            [CreatedById], 
            [Notes],
            [Version]
        )
        VALUES (
            @MenuItemId,
            @Title,
            @PreparationInstructions,
            @CookingInstructions,
            @PlatingInstructions,
            @Yield,
            @YieldPercentage,
            @PreparationTimeMinutes,
            @CookingTimeMinutes,
            GETDATE(),
            @UserId,
            @Notes,
            @CurrentVersion
        );
        
        SET @RecipeId = SCOPE_IDENTITY();
        
        -- Update menu item preparation time based on recipe
        UPDATE [dbo].[MenuItems]
        SET [PreparationTimeMinutes] = @PreparationTimeMinutes + @CookingTimeMinutes
        WHERE [Id] = @MenuItemId;
        
        COMMIT TRANSACTION;
        
        -- Return the recipe ID
        SELECT @RecipeId AS RecipeId, @IsUpdate AS IsUpdate, @CurrentVersion AS Version;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to manage recipe steps
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ManageRecipeStep]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_ManageRecipeStep]
GO

CREATE PROCEDURE [dbo].[sp_ManageRecipeStep]
    @RecipeId INT,
    @StepNumber INT,
    @Description NVARCHAR(MAX),
    @TimeRequiredMinutes INT = NULL,
    @Temperature NVARCHAR(50) = NULL,
    @SpecialEquipment NVARCHAR(100) = NULL,
    @Tips NVARCHAR(MAX) = NULL,
    @ImagePath NVARCHAR(200) = NULL,
    @IsUpdate BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- If updating, delete the existing step
        IF @IsUpdate = 1
        BEGIN
            DELETE FROM [dbo].[RecipeSteps]
            WHERE [RecipeId] = @RecipeId AND [StepNumber] = @StepNumber;
        END
        
        -- Insert the new/updated step
        INSERT INTO [dbo].[RecipeSteps] (
            [RecipeId], 
            [StepNumber], 
            [Description], 
            [TimeRequiredMinutes], 
            [Temperature], 
            [SpecialEquipment], 
            [Tips], 
            [ImagePath]
        )
        VALUES (
            @RecipeId,
            @StepNumber,
            @Description,
            @TimeRequiredMinutes,
            @Temperature,
            @SpecialEquipment,
            @Tips,
            @ImagePath
        );
        
        COMMIT TRANSACTION;
        
        -- Return success
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to get recipe details
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetRecipeByMenuItemId]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetRecipeByMenuItemId]
GO

CREATE PROCEDURE [dbo].[sp_GetRecipeByMenuItemId]
    @MenuItemId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get the recipe
    SELECT 
        r.[Id],
        r.[MenuItemId],
        r.[Title],
        r.[PreparationInstructions],
        r.[CookingInstructions],
        r.[PlatingInstructions],
        r.[Yield],
        r.[YieldPercentage],
        r.[PreparationTimeMinutes],
        r.[CookingTimeMinutes],
        r.[LastUpdated],
        r.[CreatedById],
        r.[Notes],
        r.[Version]
    FROM [dbo].[Recipes] r
    WHERE r.[MenuItemId] = @MenuItemId AND r.[IsArchived] = 0;
    
    -- Get the recipe steps
    SELECT 
        rs.[Id],
        rs.[RecipeId],
        rs.[StepNumber],
        rs.[Description],
        rs.[TimeRequiredMinutes],
        rs.[Temperature],
        rs.[SpecialEquipment],
        rs.[Tips],
        rs.[ImagePath]
    FROM [dbo].[RecipeSteps] rs
    INNER JOIN [dbo].[Recipes] r ON rs.[RecipeId] = r.[Id]
    WHERE r.[MenuItemId] = @MenuItemId AND r.[IsArchived] = 0
    ORDER BY rs.[StepNumber];
    
    -- Get the ingredients
    SELECT 
        mi.[Id],
        mi.[MenuItemId],
        mi.[IngredientId],
        i.[IngredientsName],
        i.[DisplayName],
        mi.[Quantity],
        mi.[Unit],
        mi.[IsOptional],
        mi.[Instructions],
        i.[LastPurchaseCost]
    FROM [dbo].[MenuItemIngredients] mi
    INNER JOIN [dbo].[Ingredients] i ON mi.[IngredientId] = i.[Id]
    WHERE mi.[MenuItemId] = @MenuItemId;
    
    -- Get the allergens
    SELECT 
        ma.[Id],
        ma.[MenuItemId],
        ma.[AllergenId],
        a.[Name] AS AllergenName,
        a.[Description] AS AllergenDescription,
        ma.[SeverityLevel]
    FROM [dbo].[MenuItemAllergens] ma
    INNER JOIN [dbo].[Allergens] a ON ma.[AllergenId] = a.[Id]
    WHERE ma.[MenuItemId] = @MenuItemId;
END
GO

-- Create stored procedure to manage menu item ingredients
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ManageMenuItemIngredient]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_ManageMenuItemIngredient]
GO

CREATE PROCEDURE [dbo].[sp_ManageMenuItemIngredient]
    @MenuItemId INT,
    @IngredientId INT,
    @Quantity DECIMAL(10,2),
    @Unit NVARCHAR(20),
    @IsOptional BIT = 0,
    @Instructions NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Check if ingredient already exists for this menu item
        IF EXISTS (SELECT 1 FROM [dbo].[MenuItemIngredients] WHERE [MenuItemId] = @MenuItemId AND [IngredientId] = @IngredientId)
        BEGIN
            -- Update the existing ingredient
            UPDATE [dbo].[MenuItemIngredients]
            SET 
                [Quantity] = @Quantity,
                [Unit] = @Unit,
                [IsOptional] = @IsOptional,
                [Instructions] = @Instructions
            WHERE [MenuItemId] = @MenuItemId AND [IngredientId] = @IngredientId;
        END
        ELSE
        BEGIN
            -- Insert a new ingredient
            INSERT INTO [dbo].[MenuItemIngredients] (
                [MenuItemId], 
                [IngredientId], 
                [Quantity], 
                [Unit], 
                [IsOptional], 
                [Instructions]
            )
            VALUES (
                @MenuItemId,
                @IngredientId,
                @Quantity,
                @Unit,
                @IsOptional,
                @Instructions
            );
        END
        
        COMMIT TRANSACTION;
        
        -- Return success
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to manage menu item allergens
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ManageMenuItemAllergen]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_ManageMenuItemAllergen]
GO

CREATE PROCEDURE [dbo].[sp_ManageMenuItemAllergen]
    @MenuItemId INT,
    @AllergenId INT,
    @SeverityLevel INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Check if allergen already exists for this menu item
        IF EXISTS (SELECT 1 FROM [dbo].[MenuItemAllergens] WHERE [MenuItemId] = @MenuItemId AND [AllergenId] = @AllergenId)
        BEGIN
            -- Update the existing allergen
            UPDATE [dbo].[MenuItemAllergens]
            SET [SeverityLevel] = @SeverityLevel
            WHERE [MenuItemId] = @MenuItemId AND [AllergenId] = @AllergenId;
        END
        ELSE
        BEGIN
            -- Insert a new allergen
            INSERT INTO [dbo].[MenuItemAllergens] (
                [MenuItemId], 
                [AllergenId], 
                [SeverityLevel]
            )
            VALUES (
                @MenuItemId,
                @AllergenId,
                @SeverityLevel
            );
        END
        
        COMMIT TRANSACTION;
        
        -- Return success
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Create stored procedure to get all modifiers
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetAllModifiers]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetAllModifiers]
GO

CREATE PROCEDURE [dbo].[sp_GetAllModifiers]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        [Id], 
        [Name],
        [Name] AS Description,
        [ModifierType],
        [IsActive]
    FROM [dbo].[Modifiers]
    ORDER BY [Name];
END
GO

-- Create stored procedure to get menu item version history
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetMenuItemVersionHistory]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[sp_GetMenuItemVersionHistory]
GO

CREATE PROCEDURE [dbo].[sp_GetMenuItemVersionHistory]
    @MenuItemId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        [Id],
        [MenuItemId],
        [ChangeType],
        [ChangeDate],
        [ChangedByUserId],
        [OldValue],
        [NewValue],
        [ApprovalStatus],
        [ApprovalDate],
        [ApprovedByUserId],
        [Notes]
    FROM [dbo].[MenuVersionHistory]
    WHERE [MenuItemId] = @MenuItemId
    ORDER BY [ChangeDate] DESC;
END
GO

PRINT 'Menu and Recipe Management database setup completed successfully with stored procedures.'
