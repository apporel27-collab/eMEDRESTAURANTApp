#!/bin/bash
# Script to unlock admin account

cd "$(dirname "$0")"
dotnet run --project RestaurantManagementSystem.csproj UnlockAdmin.cs
