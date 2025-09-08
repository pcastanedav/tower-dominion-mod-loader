#!/bin/bash

# Configuration
ENDPOINT="http://localhost:8080"
CODE_FILE="get-all-classes.cs"

# Check if code file exists
if [ ! -f "$CODE_FILE" ]; then
    echo "Error: $CODE_FILE not found!"
    exit 1
fi

# Read the C# code and escape it for JSON
CODE_CONTENT=$(cat "$CODE_FILE" | sed 's/\\/\\\\/g' | sed 's/"/\\"/g' | sed ':a;N;$!ba;s/\n/\\n/g')

# Create JSON payload
JSON_PAYLOAD=$(cat <<EOF
{
  "code": "$CODE_CONTENT"
}
EOF
)

# Execute the request
echo "Executing code from $CODE_FILE..."
curl -X POST \
     -H "Content-Type: application/json" \
     -d "$JSON_PAYLOAD" \
     "$ENDPOINT" \
  > all-classes.json

echo ""
echo "Request completed."
