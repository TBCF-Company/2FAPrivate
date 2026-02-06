# XML Signing Example with 2FA Authentication

Ví dụ về ứng dụng ký số XML tập trung sử dụng xác thực 2FA. Mỗi khi ký file XML, ứng dụng sẽ yêu cầu xác thực bằng mã 2 ký tự.

A centralized XML digital signature application with 2FA authentication. Every time an XML file is signed, the application requires authentication with a 2-character code.

## Overview

This example demonstrates:
- **XML Document Signing**: Sign XML documents using RSA digital signatures
- **2FA Integration**: Generate and validate 2-character authentication codes
- **Secure Workflow**: Multi-step process ensures proper authentication before signing
- **Session Management**: Temporary sessions with expiration to prevent replay attacks
- **Rate Limiting**: Maximum 3 failed attempts per session to prevent brute force attacks

**Note on 2-character codes**: This example uses 2-character codes (00-99) for simplicity and ease of use as specified in the requirements. For production environments with higher security needs, consider using 6-digit codes or TOTP-based authentication. The current implementation includes rate limiting (3 attempts) to mitigate brute force attacks.

## Architecture

### Workflow

1. **Initiate Signing** (`POST /api/XmlSigning/initiate`)
   - User submits XML content to be signed
   - System generates a 2-character code (00-99)
   - Returns the code and a session ID
   - Code displayed to user on authenticator app

2. **User Authentication**
   - User sees the 2-character code on their screen
   - User enters the code in the authenticator application

3. **Verify and Sign** (`POST /api/XmlSigning/verify-and-sign`)
   - User submits the session ID and authentication code
   - System validates the code
   - If valid, signs the XML document
   - Returns the signed XML

## Installation

### Prerequisites
- .NET 8.0 SDK or later

### Build and Run

```bash
cd NetCore/XmlSigningExample.Api
dotnet restore
dotnet build
dotnet run
```

The application will start on:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### Access Swagger UI

Navigate to `https://localhost:5001/swagger` to see the interactive API documentation and test the endpoints.

## API Endpoints

### 1. Initiate XML Signing

**Endpoint:** `POST /api/XmlSigning/initiate`

**Request Body:**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?><document><data>Example content</data></document>",
  "username": "user@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "authCode": "42",
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Please enter this 2-character code in your authenticator app to complete signing"
}
```

**Description:**
- Initiates the XML signing process
- Generates a 2-character authentication code (00-99)
- Returns a session ID to track this signing request
- Code is displayed to the user for authentication

### 2. Verify Code and Sign XML

**Endpoint:** `POST /api/XmlSigning/verify-and-sign`

**Request Body:**
```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "authCode": "42"
}
```

**Response:**
```json
{
  "success": true,
  "signedXml": "<?xml version=\"1.0\" encoding=\"utf-16\"?>\n<document>\n  <data>Example content</data>\n  <Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">...</Signature>\n</document>",
  "message": "XML document signed successfully",
  "signedAt": "2026-02-06T16:30:00Z"
}
```

**Description:**
- Verifies the authentication code
- If valid, signs the XML document with RSA signature
- Returns the signed XML with embedded signature
- Session is removed after successful signing

## Usage Example

### Using cURL

```bash
# Step 1: Initiate signing
curl -X POST https://localhost:5001/api/XmlSigning/initiate \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?><document><data>Example content</data></document>",
    "username": "user@example.com"
  }'

# Response will contain authCode (e.g., "42") and sessionId

# Step 2: User enters the code "42" in their app

# Step 3: Verify and sign
curl -X POST https://localhost:5001/api/XmlSigning/verify-and-sign \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "authCode": "42"
  }'

# Response will contain the signed XML
```

### Using C# HttpClient

```csharp
using System.Net.Http.Json;

var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

// Step 1: Initiate signing
var initiateRequest = new
{
    xmlContent = "<?xml version=\"1.0\"?><document><data>Example content</data></document>",
    username = "user@example.com"
};

var initiateResponse = await httpClient.PostAsJsonAsync("/api/XmlSigning/initiate", initiateRequest);
var authCodeResponse = await initiateResponse.Content.ReadFromJsonAsync<AuthCodeResponse>();

Console.WriteLine($"Authentication Code: {authCodeResponse.AuthCode}");
Console.WriteLine($"Session ID: {authCodeResponse.SessionId}");

// Step 2: User enters the code (simulated here)
var userEnteredCode = authCodeResponse.AuthCode; // In real app, user enters this

// Step 3: Verify and sign
var verifyRequest = new
{
    sessionId = authCodeResponse.SessionId,
    authCode = userEnteredCode
};

var signResponse = await httpClient.PostAsJsonAsync("/api/XmlSigning/verify-and-sign", verifyRequest);
var signedXmlResponse = await signResponse.Content.ReadFromJsonAsync<SignedXmlResponse>();

Console.WriteLine($"Signed XML:\n{signedXmlResponse.SignedXml}");
```

## Security Features

### Session Management
- **Session Expiry**: 5-minute timeout on pending signing sessions
- **One-time Use**: Sessions are removed after successful signing or expiration
- **Session Isolation**: Each signing request has a unique session ID
- **Automatic Cleanup**: Background task removes expired sessions to prevent memory leaks

### Authentication
- **2-Character Codes**: Simple authentication (00-99) as per requirements
- **Cryptographically Secure**: Uses `RandomNumberGenerator` for code generation
- **Code Validation**: Exact match required for authentication
- **Rate Limiting**: Maximum 3 failed verification attempts per session
- **Session Locking**: Session is locked after too many failed attempts

### XML Signing
- **RSA 2048-bit**: Strong cryptographic algorithm
- **Enveloped Signature**: Signature embedded within the XML document
- **XML Signature Standard**: Uses W3C XML Signature standard (XMLDsig)

## Production Considerations

### 1. Certificate Management
The current implementation generates a new RSA key for each signature. In production:
- Use a proper X.509 certificate from a Certificate Authority
- Store certificates securely (Azure Key Vault, HSM, etc.)
- Implement certificate rotation policies

```csharp
// Production example with certificate
var certificate = new X509Certificate2("path/to/certificate.pfx", "password");
signedXml.SigningKey = certificate.GetRSAPrivateKey();
```

### 2. Persistent Storage
Replace in-memory session storage with:
- Database (SQL Server, PostgreSQL)
- Distributed cache (Redis, Memcached)
- Ensures sessions persist across application restarts

### 3. Enhanced Authentication
Consider additional security measures:
- Increase code length (e.g., 6 digits instead of 2)
- Implement rate limiting
- Add TOTP-based codes with time synchronization
- Multi-factor authentication

### 4. Audit Logging
Log all signing operations:
- Who requested signing
- When it was signed
- Document hash/identifier
- Authentication attempts

### 5. API Security
- Add authentication (JWT, API keys)
- Implement authorization
- Use HTTPS only (disable HTTP)
- Add rate limiting
- Implement CORS policies

## Integration with 2FA App

This example can be integrated with the Flutter 2FA SDK or other authenticator apps:

1. **Display Code**: Show the 2-character code on the web interface
2. **User Action**: User sees code and enters it in their mobile app
3. **Validation**: System validates the code before proceeding with signing

## Error Handling

The API returns appropriate error messages for common scenarios:

- **Invalid XML**: "Invalid XML content: [error details]"
- **Session Not Found**: "Session not found. Please initiate signing again."
- **Session Expired**: "Session has expired. Please initiate signing again."
- **Invalid Code**: "Invalid authentication code. Please try again."

## Testing

### Sample XML Documents

```xml
<!-- Simple Document -->
<?xml version="1.0"?>
<document>
  <title>Test Document</title>
  <content>This is a test document for signing</content>
</document>
```

```xml
<!-- Invoice Example -->
<?xml version="1.0"?>
<invoice>
  <invoiceNumber>INV-2026-001</invoiceNumber>
  <date>2026-02-06</date>
  <customer>
    <name>Example Company</name>
    <email>customer@example.com</email>
  </customer>
  <items>
    <item>
      <description>Service Fee</description>
      <amount>1000.00</amount>
    </item>
  </items>
  <total>1000.00</total>
</invoice>
```

### Verify Signed XML

The signed XML includes a `<Signature>` element:

```xml
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
  <SignedInfo>
    <CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
    <SignatureMethod Algorithm="http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" />
    <Reference URI="">
      <Transforms>
        <Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" />
      </Transforms>
      <DigestMethod Algorithm="http://www.w3.org/2001/04/xmlenc#sha256" />
      <DigestValue>...</DigestValue>
    </Reference>
  </SignedInfo>
  <SignatureValue>...</SignatureValue>
</Signature>
```

## License

AGPL-3.0-or-later

## Author

Part of the 2FAPrivate project - TBCF Company
