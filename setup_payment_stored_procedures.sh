#!/bin/bash

# Define variables
SQL_DIR="/Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/SQL"
DB_SERVER="localhost"
DB_NAME="RestaurantDB"

# Function to run SQL file
run_sql_file() {
    echo "Executing $1..."
    sqlcmd -S $DB_SERVER -d $DB_NAME -i "$1" -E
    if [ $? -eq 0 ]; then
        echo "Successfully executed $1"
    else
        echo "Error executing $1"
        exit 1
    fi
}

# Execute the Payments setup script
run_sql_file "$SQL_DIR/Payments_Setup.sql"

echo "All SQL scripts executed successfully!"