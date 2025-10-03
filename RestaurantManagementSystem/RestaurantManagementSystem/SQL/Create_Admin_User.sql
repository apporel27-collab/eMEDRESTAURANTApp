-- Create_Admin_User.sql
-- Script to create an admin user with username 'admin' and password 'password'

-- Check if the admin user exists
IF EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    -- User exists, update password and unlock
    -- Use a known pre-computed hash for the password "password" with salt "abc123"
    UPDATE Users
    SET Password = '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918',
        Salt = 'abc123',
        IsLockedOut = 0,
        FailedLoginAttempts = 0
    WHERE Username = 'admin';
    
    PRINT 'Admin user updated successfully.';
END
ELSE
BEGIN
    -- User doesn't exist, create it
    -- Use a known pre-computed hash for the password "password" with salt "abc123"
    INSERT INTO Users (Username, Password, Email, FullName, RoleId, IsLockedOut, FailedLoginAttempts, Salt)
    VALUES ('admin', '1000:abc123:8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 
            'admin@restaurant.com', 'System Administrator', 1, 0, 0, 'abc123');
    
    PRINT 'Admin user created successfully.';
END

-- Output user info
SELECT Username, FullName, RoleId, IsLockedOut, FailedLoginAttempts
FROM Users
WHERE Username = 'admin';
