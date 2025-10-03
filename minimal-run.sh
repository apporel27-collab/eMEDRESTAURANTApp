#!/bin/bash

echo "Starting ultra-minimal .NET app for debugging startup issue..."

# Kill any existing processes first
pkill -f dotnet || true

# Change to project directory
cd /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem

# Set minimal environment variables
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export ASPNETCORE_URLS=http://localhost:5290

# Minimal run with timeout to prevent hanging
timeout 30s dotnet run --no-build --verbosity minimal || {
    echo "Application startup timed out or failed"
    pkill -f dotnet || true
    exit 1
}