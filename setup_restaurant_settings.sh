#!/bin/bash

# This script creates the RestaurantSettings table in the database

# Go to the project directory
cd "$(dirname "$0")"

# Get the connection string from appsettings.json
CONNECTION_STRING=$(grep -o '"DefaultConnection": *"[^"]*"' ./RestaurantManagementSystem/appsettings.json | sed 's/"DefaultConnection": *"\(.*\)"/\1/')

# Extract server name from connection string
SERVER=$(echo $CONNECTION_STRING | grep -o 'Server=[^;]*' | sed 's/Server=//')

# Extract database name from connection string
DATABASE=$(echo $CONNECTION_STRING | grep -o 'Database=[^;]*' | sed 's/Database=//' | sed 's/Initial Catalog=//')

# Extract credentials from connection string
if echo $CONNECTION_STRING | grep -q "Integrated Security=True"; then
    AUTH="-E"  # Windows Authentication
else
    # SQL Authentication
    USER=$(echo $CONNECTION_STRING | grep -o 'User ID=[^;]*' | sed 's/User ID=//')
    PASS=$(echo $CONNECTION_STRING | grep -o 'Password=[^;]*' | sed 's/Password=//')
    AUTH="-U $USER -P $PASS"
fi

echo "Creating Restaurant Settings table..."

# Execute SQL script
# Check if it's macOS and if 'sqlcmd' is available
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS - check for sqlcmd
    if command -v sqlcmd &> /dev/null; then
        # Using sqlcmd
        sqlcmd -S "$SERVER" -d "$DATABASE" $AUTH -i "./SQL/create_restaurant_settings.sql"
    else
        # Try using the docker approach if available
        if command -v docker &> /dev/null; then
            echo "sqlcmd not found, trying with Docker..."
            docker run -it --rm -v "$(pwd)/SQL:/sql" mcr.microsoft.com/mssql-tools:latest /opt/mssql-tools/bin/sqlcmd -S "$SERVER" -d "$DATABASE" $AUTH -i "/sql/create_restaurant_settings.sql"
        else
            echo "Error: sqlcmd not found and Docker not available. Please install the SQL Server command line tools."
            exit 1
        fi
    fi
else
    # Windows or Linux - assume sqlcmd is available
    sqlcmd -S "$SERVER" -d "$DATABASE" $AUTH -i "./SQL/create_restaurant_settings.sql"
fi

if [ $? -eq 0 ]; then
    echo "Restaurant Settings table created successfully."
else
    echo "Error: Failed to create Restaurant Settings table."
    exit 1
fi