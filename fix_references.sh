#!/bin/bash

# Function to replace attribute usage in all controllers
replace_attributes() {
  # Replace attribute usage in files
  find /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/Controllers -type f -name "*.cs" -exec sed -i '' \
    -e 's/\[Authorize\]/\[AuthorizeAttribute\]/g' \
    -e 's/\[HttpPost\]/\[HttpPostAttribute\]/g' \
    -e 's/\[HttpGet\]/\[HttpGetAttribute\]/g' \
    -e 's/\[ValidateAntiForgeryToken\]/\[ValidateAntiForgeryTokenAttribute\]/g' \
    -e 's/\[AllowAnonymous\]/\[AllowAnonymousAttribute\]/g' \
    -e 's/\[Route(/\[RouteAttribute(/g' \
    -e 's/\[ActionName(/\[ActionNameAttribute(/g' \
    -e 's/\[ResponseCache(/\[ResponseCacheAttribute(/g' {} \;
}

# Replace SQL references in files
replace_sql_references() {
  find /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem -type f -name "*.cs" -exec sed -i '' \
    -e 's/SqlConnection/Microsoft.Data.SqlClient.SqlConnection/g' \
    -e 's/SqlDataReader/Microsoft.Data.SqlClient.SqlDataReader/g' \
    -e 's/SqlTransaction/Microsoft.Data.SqlClient.SqlTransaction/g' \
    -e 's/SqlCommand/Microsoft.Data.SqlClient.SqlCommand/g' \
    -e 's/SqlParameter/Microsoft.Data.SqlClient.SqlParameter/g' {} \;
}

# First make the attribute replacements
echo "Replacing attribute references..."
replace_attributes

# Then replace SQL references
echo "Replacing SQL references..."
replace_sql_references

echo "Done!"
