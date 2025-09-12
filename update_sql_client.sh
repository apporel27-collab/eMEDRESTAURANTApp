#!/bin/bash

# Script to replace System.Data.SqlClient with Microsoft.Data.SqlClient
# across the entire RestaurantManagementSystem project

# Go to the project directory
cd /Users/abhikporel/dev/Restaurantapp

# Find all .cs files and replace the import statement
find RestaurantManagementSystem -name "*.cs" -type f -exec sed -i '' 's/using System\.Data\.SqlClient;/using Microsoft.Data.SqlClient;/g' {} \;

echo "SQL client imports updated across all project files."
