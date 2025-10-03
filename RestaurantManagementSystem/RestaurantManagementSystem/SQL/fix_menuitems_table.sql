-- Check if the KitchenStationId column exists in MenuItems table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'MenuItems' 
               AND COLUMN_NAME = 'KitchenStationId')
BEGIN
    -- Add the KitchenStationId column if it doesn't exist
    ALTER TABLE MenuItems
    ADD KitchenStationId INT NULL;
    
    PRINT 'Added KitchenStationId column to MenuItems table';
END
ELSE
BEGIN
    PRINT 'KitchenStationId column already exists in MenuItems table';
END
