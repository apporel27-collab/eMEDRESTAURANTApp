-- SQL script to check the current schema of RestaurantSettings table
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH, 
    IS_NULLABLE
FROM 
    INFORMATION_SCHEMA.COLUMNS
WHERE 
    TABLE_NAME = 'RestaurantSettings'
ORDER BY 
    ORDINAL_POSITION;