-- Unlock_Admin_Account.sql
-- Script to unlock the admin account and reset failed login attempts

-- Unlock the admin user account and set a known password hash
UPDATE Users 
SET IsLockedOut = 0, 
    FailedLoginAttempts = 0,
    Password = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918',
    Salt = 'abc123'
WHERE Username = 'admin';

-- Confirm the update
SELECT * ,Username, IsLockedOut, FailedLoginAttempts, Salt 
FROM Users
WHERE Username = 'admin';
