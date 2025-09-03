-- Create Recipes table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Recipes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Recipes](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [MenuItemId] [int] NOT NULL,
        [Title] [nvarchar](100) NOT NULL,
        [PreparationInstructions] [nvarchar](max) NOT NULL,
        [CookingInstructions] [nvarchar](max) NOT NULL,
        [PlatingInstructions] [nvarchar](max) NULL,
        [Yield] [int] NOT NULL DEFAULT(1),
        [YieldPercentage] [decimal](5, 2) NOT NULL DEFAULT(100.00),
        [PreparationTimeMinutes] [int] NOT NULL,
        [CookingTimeMinutes] [int] NOT NULL,
        [LastUpdated] [datetime] NOT NULL DEFAULT(getdate()),
        [CreatedById] [int] NULL,
        [Notes] [nvarchar](max) NULL,
        [IsArchived] [bit] NOT NULL DEFAULT(0),
        [Version] [int] NOT NULL DEFAULT(1),
        CONSTRAINT [PK_Recipes] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Recipes_MenuItems] FOREIGN KEY([MenuItemId]) REFERENCES [dbo].[MenuItems] ([Id])
    )
END
GO

-- Create RecipeSteps table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RecipeSteps]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RecipeSteps](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RecipeId] [int] NOT NULL,
        [StepNumber] [int] NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        [TimeRequiredMinutes] [int] NULL,
        [Temperature] [nvarchar](50) NULL,
        [SpecialEquipment] [nvarchar](255) NULL,
        [Tips] [nvarchar](max) NULL,
        [ImagePath] [nvarchar](255) NULL,
        CONSTRAINT [PK_RecipeSteps] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RecipeSteps_Recipes] FOREIGN KEY([RecipeId]) REFERENCES [dbo].[Recipes] ([Id])
    )
END
GO

PRINT 'Recipe tables have been created successfully.'
