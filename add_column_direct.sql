USE [dev_Restaurant];

-- Add GSTPercentage column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'GSTPercentage'
)
BEGIN
    ALTER TABLE MenuItems ADD GSTPercentage DECIMAL(5,2) NULL;
    PRINT 'GSTPercentage column added successfully.';
END
ELSE
BEGIN
    PRINT 'GSTPercentage column already exists.';
END
