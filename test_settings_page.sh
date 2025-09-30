#!/bin/bash

echo "Testing access to the Settings page..."
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5290/Settings)
echo "HTTP Status Code: $STATUS_CODE"

if [ "$STATUS_CODE" -eq 200 ] || [ "$STATUS_CODE" -eq 302 ]; then
    echo "Success! Settings page is accessible."
else
    echo "Error: Settings page returned status code $STATUS_CODE."
fi