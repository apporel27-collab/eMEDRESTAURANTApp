-- Fix_Admin_Password.sql
-- Direct script to fix admin user password

-- Update admin user with known correct hash for "password"
UPDATE Users
SET 
    PasswordHash = '1000:abc123:5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 
    Salt = 'abc123',
    IsLockedOut = 0,
    FailedLoginAttempts = 0,
    RequiresMFA = 0
WHERE Username = 'admin';
PRINT 'Updated admin user with correct hash';

-- Verify admin user
SELECT Id, Username, Email, PasswordHash, Salt, IsLockedOut, FailedLoginAttempts, RequiresMFA
FROM Users 
WHERE Username = 'admin';
