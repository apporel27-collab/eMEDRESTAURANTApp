-- SQL script to update the RestaurantSettings table schema to match the model

-- Check if Address column exists and rename it to StreetAddress
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'Address'
)
BEGIN
    EXEC sp_rename 'RestaurantSettings.Address', 'StreetAddress', 'COLUMN';
    PRINT 'Renamed Address column to StreetAddress.';
END

-- Check if Phone column exists and rename it to PhoneNumber
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'Phone'
)
BEGIN
    EXEC sp_rename 'RestaurantSettings.Phone', 'PhoneNumber', 'COLUMN';
    PRINT 'Renamed Phone column to PhoneNumber.';
END

-- Check if TaxRate column exists and rename it to DefaultGSTPercentage
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'TaxRate'
)
BEGIN
    EXEC sp_rename 'RestaurantSettings.TaxRate', 'DefaultGSTPercentage', 'COLUMN';
    PRINT 'Renamed TaxRate column to DefaultGSTPercentage.';
END

-- Check if Logo column exists and rename it to LogoPath
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'Logo'
)
BEGIN
    EXEC sp_rename 'RestaurantSettings.Logo', 'LogoPath', 'COLUMN';
    PRINT 'Renamed Logo column to LogoPath.';
END

-- Add missing columns
BEGIN TRY
    -- Add City column if it doesn't exist
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'City'
    )
    BEGIN
        ALTER TABLE RestaurantSettings ADD City NVARCHAR(50) NOT NULL DEFAULT 'Mumbai';
        PRINT 'Added City column.';
    END

    -- Add State column if it doesn't exist
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'State'
    )
    BEGIN
        ALTER TABLE RestaurantSettings ADD State NVARCHAR(50) NOT NULL DEFAULT 'Maharashtra';
        PRINT 'Added State column.';
    END

    -- Add Pincode column if it doesn't exist
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'Pincode'
    )
    BEGIN
        ALTER TABLE RestaurantSettings ADD Pincode NVARCHAR(10) NOT NULL DEFAULT '400001';
        PRINT 'Added Pincode column.';
    END

    -- Add Country column if it doesn't exist
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'Country'
    )
    BEGIN
        ALTER TABLE RestaurantSettings ADD Country NVARCHAR(50) NOT NULL DEFAULT 'India';
        PRINT 'Added Country column.';
    END

    -- Add GSTCode column if it doesn't exist
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'GSTCode'
    )
    BEGIN
        ALTER TABLE RestaurantSettings ADD GSTCode NVARCHAR(15) NOT NULL DEFAULT '27AAPFU0939F1ZV';
        PRINT 'Added GSTCode column.';
    END

    -- Add Website column if it doesn't exist
    IF NOT EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'Website'
    )
    BEGIN
        ALTER TABLE RestaurantSettings ADD Website NVARCHAR(100) NULL;
        PRINT 'Added Website column.';
    END

    -- Remove IsActive column if it exists
    IF EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'RestaurantSettings' AND COLUMN_NAME = 'IsActive'
    )
    BEGIN
        ALTER TABLE RestaurantSettings DROP COLUMN IsActive;
        PRINT 'Removed IsActive column.';
    END

    PRINT 'RestaurantSettings table schema updated successfully.';
END TRY
BEGIN CATCH
    PRINT 'Error updating schema: ' + ERROR_MESSAGE();
END CATCH