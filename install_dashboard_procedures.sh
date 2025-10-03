#!/bin/bash

# Script to install Home Dashboard stored procedures

echo "Installing Home Dashboard stored procedures..."

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Connection string - update this if needed
CONNECTION_STRING="Server=tcp:192.250.231.28,1433;Database=dev_Restaurant;User Id=purojit2_idmcbp;Password=45*8qce8E;Encrypt=False;TrustServerCertificate=True;Connection Timeout=60;ConnectRetryCount=3;ConnectRetryInterval=5;"

echo "Installing usp_GetHomeDashboardStats..."
sqlcmd -S "tcp:192.250.231.28,1433" -d "dev_Restaurant" -U "purojit2_idmcbp" -P "45*8qce8E" -i "$SCRIPT_DIR/RestaurantManagementSystem/RestaurantManagementSystem/SQL/usp_GetHomeDashboardStats.sql"

echo "Installing usp_GetRecentOrdersForDashboard..."
sqlcmd -S "tcp:192.250.231.28,1433" -d "dev_Restaurant" -U "purojit2_idmcbp" -P "45*8qce8E" -i "$SCRIPT_DIR/RestaurantManagementSystem/RestaurantManagementSystem/SQL/usp_GetRecentOrdersForDashboard.sql"

echo "Stored procedures installed successfully!"
echo "You can now start the application to see live dashboard data."