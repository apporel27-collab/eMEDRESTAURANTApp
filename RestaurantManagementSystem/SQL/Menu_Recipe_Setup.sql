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

PRINT 'Menu and Recipe Management database setup completed successfully.'
