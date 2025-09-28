#!/bin/bash

echo "Setting up user role management tables and stored procedures..."

# SQL Server connection parameters
SERVER="localhost"
DATABASE="Restaurant"
SQL_FILE="RestaurantManagementSystem/SQL/user_role_procedures.sql"

# Run the SQL script
echo "Running $SQL_FILE against $SERVER/$DATABASE..."
sqlcmd -S $SERVER -d $DATABASE -i "$SQL_FILE"

if [ $? -eq 0 ]; then
  echo "SQL script executed successfully."
else
  echo "Error running SQL script."
  exit 1
fi

echo "Setup complete!"
