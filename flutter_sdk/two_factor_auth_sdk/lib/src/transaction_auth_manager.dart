import 'device_activation.dart';
import 'otp_manager.dart';
import 'device_manager.dart';
import 'models/device_info.dart';

/// Transaction authentication manager for stock trading applications
/// Provides specialized methods for authenticating trading transactions
class TransactionAuthManager {
  final DeviceActivation _deviceActivation;
  final OtpManager _otpManager;
  final DeviceManager _deviceManager;

  TransactionAuthManager({
    String? serverUrl,
    DeviceActivation? deviceActivation,
    OtpManager? otpManager,
    DeviceManager? deviceManager,
  })  : _deviceActivation = deviceActivation ?? DeviceActivation(baseUrl: serverUrl),
        _otpManager = otpManager ?? OtpManager(),
        _deviceManager = deviceManager ?? DeviceManager();

  /// Verify device is authorized before allowing transactions
  /// This should be called before showing transaction screens
  Future<bool> isDeviceAuthorized() async {
    return await _deviceActivation.verifyDeviceAccess();
  }

  /// Generate OTP for transaction authentication
  /// Returns null if device is not activated
  Future<String?> generateTransactionOTP(String secret) async {
    final isActivated = await _deviceActivation.isDeviceActivated();
    if (!isActivated) {
      return null;
    }
    
    return _otpManager.generateTotp(secret);
  }

  /// Get remaining time for current OTP (in seconds)
  /// Useful for showing countdown timer to user
  int getOtpRemainingTime() {
    return _otpManager.getRemainingTime();
  }

  /// Validate transaction with OTP
  /// Used to verify user intent before executing trades
  Future<bool> validateTransaction({
    required String secret,
    required String otpCode,
  }) async {
    final isActivated = await _deviceActivation.isDeviceActivated();
    if (!isActivated) {
      return false;
    }
    
    return _otpManager.validateTotp(secret, otpCode);
  }

  /// Get device information for audit logs
  /// Include this in transaction records for security auditing
  Future<DeviceInfo> getDeviceInfo() async {
    return await _deviceManager.getDeviceInfo();
  }

  /// Request device activation for new installations
  /// Users must activate device before making transactions
  Future<Map<String, dynamic>> requestDeviceActivation({
    required String username,
    required String issuer,
  }) async {
    return await _deviceActivation.requestActivation(
      username: username,
      issuer: issuer,
    );
  }

  /// Complete device activation with OTP
  Future<bool> activateDevice({
    required String otpCode,
    required String secret,
    required String username,
    required String issuer,
  }) async {
    final response = await _deviceActivation.activateDevice(
      otpCode: otpCode,
      secret: secret,
      username: username,
      issuer: issuer,
    );
    
    return response.success;
  }

  /// Deactivate device - useful for logout or device removal
  Future<void> deactivateDevice() async {
    await _deviceActivation.deactivateDevice();
  }

  /// Generate secret for new user registration
  /// This secret should be stored securely on the server
  String generateUserSecret() {
    return _otpManager.generateSecret();
  }

  /// Create transaction signature including device ID and OTP
  /// This can be sent to server for transaction verification
  Future<Map<String, String>> createTransactionSignature({
    required String secret,
    required String transactionId,
    required String transactionType,
    required double amount,
  }) async {
    final deviceInfo = await _deviceManager.getDeviceInfo();
    final otpCode = _otpManager.generateTotp(secret);
    
    return {
      'deviceId': deviceInfo.deviceId,
      'deviceName': deviceInfo.deviceName,
      'platform': deviceInfo.platform,
      'otpCode': otpCode,
      'transactionId': transactionId,
      'transactionType': transactionType,
      'amount': amount.toString(),
      'timestamp': DateTime.now().toIso8601String(),
    };
  }
}
