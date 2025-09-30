#!/bin/bash

# Script to apply the GST column change to the MenuItems table

# Get the database connection string from appsettings.json
CONNECTION_STRING=$(grep -o '"DefaultConnection":\s*"[^"]*"' /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/appsettings.json | sed 's/"DefaultConnection":\s*"\(.*\)"/\1/')

# Extract the database name from the connection string
DB_NAME=$(echo $CONNECTION_STRING | grep -o "Initial Catalog=[^;]*" | sed 's/Initial Catalog=//')

# If the database name is not found with Initial Catalog, try Database=
if [ -z "$DB_NAME" ]; then
    DB_NAME=$(echo $CONNECTION_STRING | grep -o "Database=[^;]*" | sed 's/Database=//')
fi

# Replace the database name in the SQL script
sed -i "" "s/\[YourDatabaseName\]/\[$DB_NAME\]/g" /Users/abhikporel/dev/Restaurantapp/add_gst_to_menuitems.sql

echo "Executing SQL script to add GST column to MenuItems table..."

# Execute the SQL script
# Note: You may need to adjust this command based on your SQL client and authentication method
sqlcmd -S localhost -d $DB_NAME -i /Users/abhikporel/dev/Restaurantapp/add_gst_to_menuitems.sql

echo "SQL script execution completed."