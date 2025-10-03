-- Disable_MFA_For_Admin.sql
-- Direct script to disable MFA for admin user

-- Update admin user to disable MFA
UPDATE Users
SET 
    RequiresMFA = 0
WHERE Username = 'admin';
PRINT 'Disabled MFA for admin user';

-- Verify admin user
SELECT Id, Username, Email, PasswordHash, Salt, IsLockedOut, FailedLoginAttempts, RequiresMFA
FROM Users 
WHERE Username = 'admin';
