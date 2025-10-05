-- =============================================
-- SubCategories Table Creation Script
-- Description: Creates SubCategories table with foreign key relationship to Categories
-- Author: Restaurant Management System
-- Date: October 5, 2025
-- =============================================

-- Drop table if exists (for re-running the script)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SubCategories]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[SubCategories]
    PRINT 'Existing SubCategories table dropped.'
END

-- Create SubCategories Table
CREATE TABLE [dbo].[SubCategories] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL DEFAULT(1),
    [CategoryId] int NOT NULL,
    [DisplayOrder] int NOT NULL DEFAULT(0),
    [CreatedAt] datetime2 NOT NULL DEFAULT(GETDATE()),
    [UpdatedAt] datetime2 NULL,
    
    CONSTRAINT [PK_SubCategories] PRIMARY KEY CLUSTERED ([Id] ASC)
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, 
              ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

PRINT 'SubCategories table created successfully.'

-- Add Foreign Key Constraint
ALTER TABLE [dbo].[SubCategories] WITH CHECK 
ADD CONSTRAINT [FK_SubCategories_Categories_CategoryId] 
FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories] ([Id])
ON DELETE NO ACTION

ALTER TABLE [dbo].[SubCategories] CHECK CONSTRAINT [FK_SubCategories_Categories_CategoryId]

PRINT 'Foreign key constraint added successfully.'

-- Create Indexes for better performance
CREATE NONCLUSTERED INDEX [IX_SubCategories_CategoryId] 
ON [dbo].[SubCategories] ([CategoryId] ASC)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
      DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_SubCategories_IsActive] 
ON [dbo].[SubCategories] ([IsActive] ASC)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
      DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_SubCategories_DisplayOrder] 
ON [dbo].[SubCategories] ([DisplayOrder] ASC)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
      DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

PRINT 'Performance indexes created successfully.'

-- Insert Sample Data
INSERT INTO [dbo].[SubCategories] ([Name], [Description], [CategoryId], [IsActive], [DisplayOrder], [CreatedAt], [UpdatedAt])
VALUES 
    ('Hot Appetizers', 'Warm appetizer dishes', 1, 1, 1, '2024-01-01 12:00:00', NULL),
    ('Cold Appetizers', 'Cold appetizer dishes', 1, 1, 2, '2024-01-01 12:00:00', NULL),
    ('Salads', 'Fresh salads and greens', 1, 1, 3, '2024-01-01 12:00:00', NULL),
    ('Meat Dishes', 'Meat-based main courses', 2, 1, 1, '2024-01-01 12:00:00', NULL),
    ('Vegetarian Dishes', 'Vegetarian main courses', 2, 1, 2, '2024-01-01 12:00:00', NULL),
    ('Seafood', 'Fresh seafood dishes', 2, 1, 3, '2024-01-01 12:00:00', NULL),
    ('Pasta & Rice', 'Pasta and rice dishes', 2, 1, 4, '2024-01-01 12:00:00', NULL),
    ('Cakes', 'Various types of cakes', 3, 1, 1, '2024-01-01 12:00:00', NULL),
    ('Ice Cream', 'Ice cream desserts', 3, 1, 2, '2024-01-01 12:00:00', NULL),
    ('Pastries', 'Sweet pastries and baked goods', 3, 1, 3, '2024-01-01 12:00:00', NULL),
    ('Hot Beverages', 'Coffee, tea, hot chocolate', 4, 1, 1, '2024-01-01 12:00:00', NULL),
    ('Cold Beverages', 'Juices, sodas, iced drinks', 4, 1, 2, '2024-01-01 12:00:00', NULL),
    ('Alcoholic Beverages', 'Wine, beer, cocktails', 4, 1, 3, '2024-01-01 12:00:00', NULL);

PRINT 'Sample data inserted successfully.'

-- Verification Query
SELECT 
    sc.Id,
    sc.Name AS SubCategoryName,
    sc.Description,
    c.Name AS CategoryName,
    sc.DisplayOrder,
    sc.IsActive,
    sc.CreatedAt
FROM [dbo].[SubCategories] sc
INNER JOIN [dbo].[Categories] c ON sc.CategoryId = c.Id
ORDER BY c.Name, sc.DisplayOrder;

PRINT 'SubCategories table setup completed successfully!'
PRINT 'Total SubCategories created: ' + CAST(@@ROWCOUNT AS NVARCHAR(10))

-- Show table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'SubCategories'
ORDER BY ORDINAL_POSITION;

PRINT 'SubCategories table structure displayed above.'