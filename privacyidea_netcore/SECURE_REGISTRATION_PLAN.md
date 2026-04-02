# 📱 Plan: Secure Token Registration với RSA Key Exchange

## ✅ IMPLEMENTATION STATUS: COMPLETED

---

## 🔐 Luồng Hoạt Động (Đã Triển Khai)

```
┌─────────────────────────────────────────────────────────────────┐
│                          CLIENT (Mobile App)                      │
├─────────────────────────────────────────────────────────────────┤
│ 1. Generate RSA Key Pair (2048-bit)                              │
│    - Private Key → Lưu an toàn trong Keychain/Keystore          │
│    - Public Key → Gửi lên server                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼ POST /register/secure
┌─────────────────────────────────────────────────────────────────┐
│ {                                                                │
│   "type": "totp",                                                │
│   "publicKey": "MIIBIjAN...base64...",                          │
│   "user": "john.doe",                                            │
│   "deviceId": "device-uuid",                                     │
│   "deviceInfo": { "model": "iPhone 15", "os": "iOS 17" }        │
│ }                                                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                          SERVER (PrivacyIDEA)                     │
├─────────────────────────────────────────────────────────────────┤
│ 2. Validate RSA public key (min 2048 bits)                      │
│ 3. Generate TOTP seed (20/32/64 bytes based on algorithm)      │
│ 4. Encrypt seed with RSA-OAEP-SHA256                            │
│ 5. Create token in database                                      │
│ 6. Store device binding (optional)                              │
│ 7. Return encrypted seed                                         │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼ Response
┌─────────────────────────────────────────────────────────────────┐
│ {                                                                │
│   "status": true,                                                │
│   "serial": "TOTP00012345",                                      │
│   "encryptedSeed": "base64-encrypted-data...",                  │
│   "algorithm": "sha1", "digits": 6, "period": 30                │
│ }                                                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                          CLIENT (Mobile App)                      │
│ 8. Decrypt seed = RSA_OAEP_Decrypt(encryptedSeed, privateKey)   │
│ 9. Store seed securely in Keychain/Keystore                     │
│ 10. Generate TOTP codes using seed                              │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ Completed Tasks

### Phase 1: CryptoService RSA-OAEP ✅
- [x] `RsaEncryptOaep(data, publicKey)` - RSA-OAEP-SHA256 encryption
- [x] `RsaDecryptOaep(ciphertext, privateKey)` - RSA-OAEP-SHA256 decryption
- [x] `ImportRsaPublicKeyFromPem(pem)` - Parse PEM format (PKCS#1 & SPKI)
- [x] `ImportRsaPublicKeyFromBase64(base64)` - Parse Base64 DER format
- [x] `ValidateRsaPublicKey(key, minSize)` - Validate key format & size

### Phase 2: DTOs ✅
- [x] `SecureRegisterRequest` - Request với publicKey, deviceInfo
- [x] `SecureRegisterResponse` - Response với encryptedSeed
- [x] `DeviceInfo` - Device model, OS, version, fingerprint
- [x] `SecureRegistrationConfig` - Configuration options

### Phase 3: API Endpoint ✅
- [x] `POST /register/secure` - Main secure registration endpoint
- [x] `GET /register/secure/info` - Get registration capabilities
- [x] RSA public key validation (format, size)
- [x] Seed generation (20/32/64 bytes based on hash algorithm)
- [x] RSA-OAEP encryption of seed
- [x] Token creation with custom seed
- [x] Audit logging

### Phase 4: Device Binding ✅
- [x] `TokenDeviceBinding` entity with full tracking
- [x] DbContext registration
- [x] PostgreSQL schema support

---

## 📁 Files Created/Modified

### Created:
- `src/PrivacyIDEA.Api/Models/SecureRegistration.cs`
- `src/PrivacyIDEA.Domain/Entities/TokenDeviceBinding.cs`

### Modified:
- `src/PrivacyIDEA.Core/Interfaces/ICryptoService.cs` - Added RSA-OAEP methods
- `src/PrivacyIDEA.Core/Services/CryptoService.cs` - Implemented RSA-OAEP
- `src/PrivacyIDEA.Core/Interfaces/ITokenService.cs` - Extended TokenInitRequest
- `src/PrivacyIDEA.Api/Controllers/RegisterController.cs` - Added /register/secure
- `src/PrivacyIDEA.Infrastructure/Data/PrivacyIdeaDbContext.cs` - Added TokenDeviceBindings

---

## 🔧 API Usage

### Request
```http
POST /register/secure
Content-Type: application/json

{
  "publicKey": "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...",
  "type": "totp",
  "user": "john.doe",
  "realm": "default",
  "algorithm": "sha1",
  "digits": 6,
  "period": 30,
  "deviceId": "550e8400-e29b-41d4-a716-446655440000",
  "deviceInfo": {
    "model": "iPhone 15 Pro",
    "os": "iOS",
    "osVersion": "17.4",
    "appVersion": "1.0.0"
  }
}
```

### Response
```json
{
  "status": true,
  "serial": "TOTP00012345",
  "type": "totp",
  "encryptedSeed": "base64-encoded-rsa-encrypted-seed...",
  "algorithm": "sha1",
  "digits": 6,
  "period": 30,
  "issuer": "PrivacyIDEA",
  "deviceBindingId": "abc123def456"
}
```

---

## 🔒 Security Features

| Feature | Status |
|---------|--------|
| RSA-OAEP-SHA256 | ✅ Implemented |
| Minimum 2048-bit key | ✅ Enforced |
| Seed never in plaintext | ✅ Always encrypted |
| Device binding | ✅ Optional |
| Audit logging | ✅ Full tracking |
| Rate limiting | 🟡 Configurable |

---

## 📱 Client Implementation Reference

### iOS (Swift)
```swift
let privateKey = try SecKeyCreateRandomKey([
    kSecAttrKeyType: kSecAttrKeyTypeRSA,
    kSecAttrKeySizeInBits: 2048
] as CFDictionary, nil)!

let publicKeyData = SecKeyCopyExternalRepresentation(
    SecKeyCopyPublicKey(privateKey)!, nil)!

// After receiving response
let seed = SecKeyCreateDecryptedData(
    privateKey, .rsaEncryptionOAEPSHA256,
    encryptedSeed as CFData, nil)!
```

### Android (Kotlin)
```kotlin
val keyPair = KeyPairGenerator.getInstance("RSA").apply {
    initialize(2048)
}.generateKeyPair()

val publicKeyBase64 = Base64.encodeToString(
    keyPair.public.encoded, Base64.NO_WRAP)

// After receiving response
val cipher = Cipher.getInstance("RSA/ECB/OAEPWithSHA-256AndMGF1Padding")
cipher.init(Cipher.DECRYPT_MODE, keyPair.private)
val seed = cipher.doFinal(Base64.decode(encryptedSeed, Base64.NO_WRAP))
```

---

*Implementation completed: 02/04/2026*
