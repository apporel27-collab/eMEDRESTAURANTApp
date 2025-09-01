-- Check the structure of the Users table
SELECT TOP 1 * FROM Users;

-- Check if there are any users with Role = 2 (Server role)
SELECT * FROM Users WHERE Role = 2 AND IsActive = 1;

-- Check if there's a FullName column or if we need to concatenate FirstName and LastName
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users';
