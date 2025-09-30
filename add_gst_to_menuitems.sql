-- SQL Script to add the GST column to the MenuItems table
USE [dev_Restaurant]; -- Database name from connection string

-- Check if the GSTPercentage column already exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'GSTPercentage')
BEGIN
    -- Add the GSTPercentage column
    ALTER TABLE MenuItems
    ADD GSTPercentage DECIMAL(5,2) NULL;

    -- Set default value for existing records
    UPDATE MenuItems
    SET GSTPercentage = 5.00;

    PRINT 'GSTPercentage column added to MenuItems table';
END
ELSE
BEGIN
    PRINT 'GSTPercentage column already exists in MenuItems table';
END