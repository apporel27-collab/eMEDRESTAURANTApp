-- Add missing columns to MenuItems table
-- This script checks if the columns exist and adds them if they don't
-- Run this script to fix the missing columns error when creating menu items

-- Check if PreparationTimeMinutes column exists, add it if it doesn't
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'PreparationTimeMinutes'
)
BEGIN
    ALTER TABLE MenuItems
    ADD PreparationTimeMinutes INT NOT NULL DEFAULT 15;
    
    PRINT 'Added PreparationTimeMinutes column to MenuItems table';
END
ELSE
    PRINT 'PreparationTimeMinutes column already exists';

-- Check if KitchenStationId column exists, add it if it doesn't
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'KitchenStationId'
)
BEGIN
    ALTER TABLE MenuItems
    ADD KitchenStationId INT NULL;
    
    PRINT 'Added KitchenStationId column to MenuItems table';
END
ELSE
    PRINT 'KitchenStationId column already exists';

-- Check if CalorieCount column exists, add it if it doesn't
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'CalorieCount'
)
BEGIN
    ALTER TABLE MenuItems
    ADD CalorieCount INT NULL;
    
    PRINT 'Added CalorieCount column to MenuItems table';
END
ELSE
    PRINT 'CalorieCount column already exists';

-- Check if IsFeatured column exists, add it if it doesn't
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'IsFeatured'
)
BEGIN
    ALTER TABLE MenuItems
    ADD IsFeatured BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added IsFeatured column to MenuItems table';
END
ELSE
    PRINT 'IsFeatured column already exists';

-- Check if IsSpecial column exists, add it if it doesn't
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'IsSpecial'
)
BEGIN
    ALTER TABLE MenuItems
    ADD IsSpecial BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added IsSpecial column to MenuItems table';
END
ELSE
    PRINT 'IsSpecial column already exists';

-- Check if DiscountPercentage column exists, add it if it doesn't
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'DiscountPercentage'
)
BEGIN
    ALTER TABLE MenuItems
    ADD DiscountPercentage DECIMAL(5,2) NULL;
    
    PRINT 'Added DiscountPercentage column to MenuItems table';
END
ELSE
    PRINT 'DiscountPercentage column already exists';

-- Print complete message
PRINT 'Schema update complete. All missing columns have been added to the MenuItems table.';
