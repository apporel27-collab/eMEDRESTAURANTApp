-- Add SubCategoryId column to MenuItems table
-- Run this script to add SubCategory support to existing MenuItems

USE [RestaurantDB]
GO

-- Check if the SubCategoryId column doesn't exist before adding it
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'SubCategoryId')
BEGIN
    -- Add SubCategoryId column as nullable foreign key
    ALTER TABLE MenuItems
    ADD SubCategoryId INT NULL;
    
    PRINT 'SubCategoryId column added to MenuItems table successfully.'
END
ELSE
BEGIN
    PRINT 'SubCategoryId column already exists in MenuItems table.'
END

-- Check if SubCategories table exists before creating foreign key
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubCategories')
BEGIN
    -- Add foreign key constraint if it doesn't exist
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
                   WHERE CONSTRAINT_NAME = 'FK_MenuItems_SubCategoryId')
    BEGIN
        ALTER TABLE MenuItems
        ADD CONSTRAINT FK_MenuItems_SubCategoryId 
        FOREIGN KEY (SubCategoryId) REFERENCES SubCategories(Id)
        ON DELETE SET NULL;
        
        PRINT 'Foreign key constraint FK_MenuItems_SubCategoryId added successfully.'
    END
    ELSE
    BEGIN
        PRINT 'Foreign key constraint FK_MenuItems_SubCategoryId already exists.'
    END
END
ELSE
BEGIN
    PRINT 'SubCategories table does not exist. Please create SubCategories table first using create_subcategories_table.sql'
END

-- Create index for better performance on SubCategoryId lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MenuItems_SubCategoryId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MenuItems_SubCategoryId
    ON MenuItems (SubCategoryId)
    INCLUDE (CategoryId, Name, IsAvailable);
    
    PRINT 'Index IX_MenuItems_SubCategoryId created successfully.'
END
ELSE
BEGIN
    PRINT 'Index IX_MenuItems_SubCategoryId already exists.'
END

PRINT 'MenuItem SubCategory integration setup completed successfully!'