-- Fix_Auth_Setup.sql
-- Script to properly create authentication tables with correct data types

-- Drop existing tables if they exist
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    -- Check if we need to create a backup
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users_Backup')
    BEGIN
        SELECT * INTO Users_Backup FROM Users;
        PRINT 'Created backup of Users table';
    END
END

-- Create User table if it doesn't exist or has wrong schema
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserId INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(50) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        RoleId INT NOT NULL,
        Salt NVARCHAR(50) NULL,
        IsLockedOut BIT NOT NULL DEFAULT 0,
        FailedLoginAttempts INT NOT NULL DEFAULT 0,
        CreatedDate DATETIME DEFAULT GETDATE()
    );
    PRINT 'Users table created';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Salt')
BEGIN
    -- Add Salt column if it doesn't exist
    ALTER TABLE Users ADD Salt NVARCHAR(50) NULL;
    PRINT 'Added Salt column to Users table';
END

-- Create Roles table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        RoleId INT PRIMARY KEY IDENTITY(1,1),
        RoleName NVARCHAR(50) NOT NULL UNIQUE
    );
    PRINT 'Roles table created';
    
    -- Insert default roles
    INSERT INTO Roles (RoleName) VALUES ('Administrator');
    INSERT INTO Roles (RoleName) VALUES ('Manager');
    INSERT INTO Roles (RoleName) VALUES ('Staff');
    PRINT 'Default roles created';
END

-- Create admin user if it doesn't exist
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Password, Email, FullName, RoleId, IsLockedOut, FailedLoginAttempts)
    VALUES ('admin', 'password', 'admin@restaurant.com', 'System Administrator', 1, 0, 0);
    PRINT 'Admin user created';
END
ELSE
BEGIN
    -- Reset admin user password and unlock account
    UPDATE Users
    SET Password = 'password',
        IsLockedOut = 0,
        FailedLoginAttempts = 0
    WHERE Username = 'admin';
    PRINT 'Admin user reset';
END
