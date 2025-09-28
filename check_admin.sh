#!/bin/bash

# Compile and run the AdminCheck.cs file
cd /Users/abhikporel/dev/Restaurantapp/
dotnet new console -o TempAdminCheck --force
cp AdminCheck.cs TempAdminCheck/Program.cs
cd TempAdminCheck
dotnet add package Microsoft.Data.SqlClient
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add package BCrypt.Net-Next
cp ../RestaurantManagementSystem/appsettings.json ./
dotnet run

# Clean up
cd ..
rm -rf TempAdminCheck
