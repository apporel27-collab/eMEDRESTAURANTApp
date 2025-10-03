-- Script to fix duplicate columns in Users table and ensure proper schema
-- First check the current structure
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY COLUMN_NAME;

-- Check for duplicate column names
SELECT 
    COLUMN_NAME,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
GROUP BY COLUMN_NAME
HAVING COUNT(*) > 1;

-- Check existing admin user
SELECT * FROM Users WHERE Username = 'admin';

-- Ensure we have PasswordHash column (not Password)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Password') 
    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordHash')
BEGIN
    -- We have both columns - update PasswordHash with Password values if needed
    UPDATE Users 
    SET PasswordHash = Password 
    WHERE PasswordHash IS NULL AND Password IS NOT NULL;
    
    -- Then drop the Password column
    ALTER TABLE Users DROP COLUMN Password;
    PRINT 'Dropped duplicate Password column';
END

-- Update admin user with known correct hash
UPDATE Users
SET 
    PasswordHash = '1000:abc123:5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 
    Salt = 'abc123',
    IsLockedOut = 0,
    FailedLoginAttempts = 0
WHERE Username = 'admin';
PRINT 'Updated admin user with correct hash';

-- Verify admin user
SELECT Id, Username, Email, PasswordHash, Salt, IsLockedOut, FailedLoginAttempts 
FROM Users 
WHERE Username = 'admin';
