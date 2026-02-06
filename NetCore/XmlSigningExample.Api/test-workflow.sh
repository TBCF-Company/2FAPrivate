#!/bin/bash
# Test script for XML Signing Example with 2FA Authentication
# This script demonstrates the complete workflow

echo "=== XML Signing Example - Complete Workflow Test ==="
echo ""

BASE_URL="http://localhost:5202"

# Sample XML content
XML_CONTENT='<?xml version="1.0"?><invoice><number>INV-2026-001</number><customer>John Doe</customer><amount>1000.00</amount></invoice>'

echo "Step 1: Initiating XML signing..."
echo "-------------------------------"
echo "XML Content: $XML_CONTENT"
echo ""

# Step 1: Initiate signing
RESPONSE=$(curl -s -X POST "$BASE_URL/api/XmlSigning/initiate" \
  -H "Content-Type: application/json" \
  -d "{
    \"xmlContent\": \"$XML_CONTENT\",
    \"username\": \"john.doe@example.com\"
  }")

echo "Response:"
echo "$RESPONSE" | jq .
echo ""

# Extract session ID and auth code
SESSION_ID=$(echo "$RESPONSE" | jq -r '.sessionId')
AUTH_CODE=$(echo "$RESPONSE" | jq -r '.authCode')

echo "Step 2: User sees authentication code"
echo "--------------------------------------"
echo "🔐 Authentication Code: $AUTH_CODE"
echo "📱 User enters this code in their authenticator app"
echo "⏱️  Session ID: $SESSION_ID"
echo ""

read -p "Press Enter to continue with code verification..."
echo ""

echo "Step 3: Verifying code and signing XML..."
echo "----------------------------------------"

# Step 2: Verify and sign
SIGNED_RESPONSE=$(curl -s -X POST "$BASE_URL/api/XmlSigning/verify-and-sign" \
  -H "Content-Type: application/json" \
  -d "{
    \"sessionId\": \"$SESSION_ID\",
    \"authCode\": \"$AUTH_CODE\"
  }")

SUCCESS=$(echo "$SIGNED_RESPONSE" | jq -r '.success')
MESSAGE=$(echo "$SIGNED_RESPONSE" | jq -r '.message')

echo "Success: $SUCCESS"
echo "Message: $MESSAGE"
echo ""

if [ "$SUCCESS" = "true" ]; then
    echo "✅ XML Signed Successfully!"
    echo ""
    echo "Signed XML (formatted):"
    echo "----------------------"
    echo "$SIGNED_RESPONSE" | jq -r '.signedXml' | xmllint --format - 2>/dev/null || echo "$SIGNED_RESPONSE" | jq -r '.signedXml'
    echo ""
    
    SIGNED_AT=$(echo "$SIGNED_RESPONSE" | jq -r '.signedAt')
    echo "Signed at: $SIGNED_AT"
else
    echo "❌ Signing Failed: $MESSAGE"
fi

echo ""
echo "=== Test Complete ==="
echo ""
echo "Additional Tests:"
echo "1. Test with invalid code: curl -X POST $BASE_URL/api/XmlSigning/verify-and-sign -H 'Content-Type: application/json' -d '{\"sessionId\":\"$SESSION_ID\",\"authCode\":\"99\"}'"
echo "2. Test with expired session: Wait 5 minutes and retry"
echo "3. Test rate limiting: Try wrong code 3 times"
echo ""
echo "Access Swagger UI: http://localhost:5202/swagger"
