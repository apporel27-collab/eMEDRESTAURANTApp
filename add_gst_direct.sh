#!/bin/bash

echo "Checking MenuItems table schema..."

# Connection parameters from appsettings.json
SERVER="tcp:192.250.231.28,1433"
DATABASE="dev_Restaurant"
USERNAME="purojit2_idmcbp"
PASSWORD="45*8qce8E"

# Execute SQL script to check table schema
echo "Executing SQL script to check table schema..."
/opt/homebrew/bin/sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -C -i "check_gst_column.sql"

echo "Now attempting to add GSTPercentage column directly..."

# Create a temporary SQL file for direct execution
cat > add_column_direct.sql << EOF
USE [dev_Restaurant];

-- Add GSTPercentage column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'GSTPercentage'
)
BEGIN
    ALTER TABLE MenuItems ADD GSTPercentage DECIMAL(5,2) NULL;
    PRINT 'GSTPercentage column added successfully.';
END
ELSE
BEGIN
    PRINT 'GSTPercentage column already exists.';
END
EOF

# Execute the direct SQL command
/opt/homebrew/bin/sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -C -i "add_column_direct.sql"

echo "Done!"