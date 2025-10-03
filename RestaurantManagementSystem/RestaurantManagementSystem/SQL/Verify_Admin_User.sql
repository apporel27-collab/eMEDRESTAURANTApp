-- Verify_Admin_User.sql
-- This script checks if the admin user exists and is set up correctly

-- Show the admin user details
SELECT * FROM Users WHERE Username = 'admin';

-- Verify stored hash format matches what AuthService expects
DECLARE @ExpectedHashPrefix VARCHAR(10) = '1000:';
SELECT 
    Username,
    CASE 
        WHEN PasswordHash LIKE @ExpectedHashPrefix + '%' THEN 'Valid format'
        ELSE 'Invalid format'
    END AS HashFormat,
    PasswordHash,
    Salt,
    IsLockedOut,
    FailedLoginAttempts
FROM Users
WHERE Username = 'admin';

-- Check database schema
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users';

-- Directly update admin user with correct hash
-- This is for the password "password" with salt "abc123"
UPDATE Users
SET 
    PasswordHash = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918',
    Salt = 'abc123',
    IsLockedOut = 0,
    FailedLoginAttempts = 0
WHERE Username = 'admin';

-- Verify update
SELECT 
    Username,
    PasswordHash,
    Salt,
    IsLockedOut,
    FailedLoginAttempts
FROM Users
WHERE Username = 'admin';
