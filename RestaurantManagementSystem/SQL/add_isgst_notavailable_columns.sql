-- Adds IsGstApplicable and NotAvailable columns to MenuItems if they do not already exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'IsGstApplicable')
BEGIN
    ALTER TABLE MenuItems ADD IsGstApplicable BIT NOT NULL DEFAULT 1;
    PRINT 'Added IsGstApplicable column to MenuItems.';
END
ELSE
BEGIN
    PRINT 'IsGstApplicable column already exists.';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'NotAvailable')
BEGIN
    ALTER TABLE MenuItems ADD NotAvailable BIT NOT NULL DEFAULT 0;
    PRINT 'Added NotAvailable column to MenuItems.';
END
ELSE
BEGIN
    PRINT 'NotAvailable column already exists.';
END
