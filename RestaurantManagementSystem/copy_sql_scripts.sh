#!/bin/bash
# This script copies the SQL scripts to the output directory

echo "Copying SQL scripts to output directory..."

# Get the current directory
sourceDir=$(dirname "$0")
target="$sourceDir/bin/Debug/net6.0/SQL/"

# Create the target directory if it doesn't exist
mkdir -p "$target"

# Copy the files
cp "$sourceDir/SQL/Fix_Auth_Setup.sql" "$target"
cp "$sourceDir/SQL/Create_Admin_User.sql" "$target"

echo "SQL scripts copy complete!"
