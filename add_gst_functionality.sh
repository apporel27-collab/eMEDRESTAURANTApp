#!/bin/bash

echo "Updating Restaurant Management System with GST functionality..."

# Get the directory where the script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if connection string is provided as an argument
if [ "$1" == "" ]; then
    echo "Please provide the database connection string as an argument."
    echo "Usage: $0 \"Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;\""
    exit 1
fi

CONNECTION_STRING="$1"

# Step 1: Add GST column to database
echo "Step 1: Adding GST column to the MenuItems table..."
"$SCRIPT_DIR/apply_gst_column.sh" "$CONNECTION_STRING"
if [ $? -ne 0 ]; then
    echo "Failed to add GST column to database. Please check the error message above."
    exit 1
fi
echo "✓ GST column added successfully to the database."

# Step 2: Build the application
echo "Step 2: Building the application..."
cd "$SCRIPT_DIR"
dotnet build RestaurantManagementSystem/RestaurantManagementSystem.csproj
if [ $? -ne 0 ]; then
    echo "Failed to build the application. Please check the error message above."
    exit 1
fi
echo "✓ Application built successfully."

echo ""
echo "GST functionality has been successfully added to the Restaurant Management System."
echo "You can now run the application with 'dotnet run --project RestaurantManagementSystem/RestaurantManagementSystem.csproj'"
echo ""
echo "Summary of changes:"
echo "1. Added GSTPercentage property to MenuItem.cs model"
echo "2. Added GSTPercentage property to MenuItemViewModel.cs viewmodel"
echo "3. Added GSTPercentage field to the database schema"
echo "4. Updated the Create/Edit forms to include GST Percentage input"
echo "5. Updated the Details view to display GST Percentage"