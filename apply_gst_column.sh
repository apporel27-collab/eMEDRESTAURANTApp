#!/bin/bash

# Get the directory where the script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if connection string is provided as an argument
if [ "$1" == "" ]; then
    echo "Please provide the database connection string as an argument."
    echo "Usage: $0 \"Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;\""
    exit 1
fi

CONNECTION_STRING="$1"

# Extract server, database, username and password from connection string
SERVER=$(echo "$CONNECTION_STRING" | grep -oP 'Server=\K[^;]+')
DATABASE=$(echo "$CONNECTION_STRING" | grep -oP 'Database=\K[^;]+')
USERNAME=$(echo "$CONNECTION_STRING" | grep -oP 'User Id=\K[^;]+')
PASSWORD=$(echo "$CONNECTION_STRING" | grep -oP 'Password=\K[^;]+')

# Ensure all variables are set
if [ -z "$SERVER" ] || [ -z "$DATABASE" ] || [ -z "$USERNAME" ] || [ -z "$PASSWORD" ]; then
    echo "Error: Could not extract all required connection parameters."
    exit 1
fi

# Get SQL script path
SQL_SCRIPT="$SCRIPT_DIR/add_gst_to_menuitems.sql"

# Replace database name placeholder
sed -i '' "s/YourDatabaseName/$DATABASE/g" "$SQL_SCRIPT"

# Execute SQL script
echo "Adding GST column to MenuItems table..."
/opt/homebrew/bin/sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -i "$SQL_SCRIPT"

if [ $? -eq 0 ]; then
    echo "GST column added successfully."
else
    echo "Failed to execute SQL script. Check the connection details and try again."
    exit 1
fi

echo "Done!"