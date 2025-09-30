-- SQL Script to check if GSTPercentage column exists
USE [dev_Restaurant];

-- List all columns in the MenuItems table
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'MenuItems';