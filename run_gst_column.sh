#!/bin/bash

echo "Adding GST column to MenuItems table..."

# Connection parameters from appsettings.json
SERVER="tcp:192.250.231.28,1433"
DATABASE="dev_Restaurant"
USERNAME="purojit2_idmcbp"
PASSWORD="45*8qce8E"

# Execute SQL script directly using sqlcmd with Trust Server Certificate
echo "Executing SQL script to add the GSTPercentage column..."
/opt/homebrew/bin/sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -C -i "add_gst_to_menuitems.sql"

if [ $? -eq 0 ]; then
    echo "GST column added successfully to MenuItems table."
else
    echo "Error: Failed to add GST column to MenuItems table."
    exit 1
fi

echo "Done!"