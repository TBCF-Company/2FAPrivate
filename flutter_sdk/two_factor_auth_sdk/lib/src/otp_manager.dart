import 'package:otp/otp.dart';
import 'dart:convert';
import 'dart:math';
import 'package:crypto/crypto.dart';

/// OTP Manager for generating and validating OTP codes
class OtpManager {
  /// Generate TOTP code
  String generateTotp(
    String secret, {
    int digits = 6,
    int period = 30,
    Algorithm algorithm = Algorithm.SHA1,
  }) {
    try {
      final code = OTP.generateTOTPCodeString(
        secret,
        DateTime.now().millisecondsSinceEpoch,
        length: digits,
        interval: period,
        algorithm: algorithm,
        isGoogle: true,
      );
      return code;
    } catch (e) {
      print('Error generating TOTP: $e');
      return '000000';
    }
  }

  /// Generate HOTP code
  String generateHotp(
    String secret,
    int counter, {
    int digits = 6,
    Algorithm algorithm = Algorithm.SHA1,
  }) {
    try {
      final code = OTP.generateHOTPCodeString(
        secret,
        counter,
        length: digits,
        algorithm: algorithm,
        isGoogle: true,
      );
      return code;
    } catch (e) {
      print('Error generating HOTP: $e');
      return '000000';
    }
  }

  /// Validate TOTP code
  bool validateTotp(
    String secret,
    String code, {
    int digits = 6,
    int period = 30,
    int window = 1,
    Algorithm algorithm = Algorithm.SHA1,
  }) {
    try {
      final currentTime = DateTime.now().millisecondsSinceEpoch;
      
      // Check current time step and adjacent time steps (window)
      for (int i = -window; i <= window; i++) {
        final timeOffset = currentTime + (i * period * 1000);
        final expectedCode = OTP.generateTOTPCodeString(
          secret,
          timeOffset,
          length: digits,
          interval: period,
          algorithm: algorithm,
          isGoogle: true,
        );
        
        if (expectedCode == code) {
          return true;
        }
      }
      
      return false;
    } catch (e) {
      print('Error validating TOTP: $e');
      return false;
    }
  }

  /// Validate HOTP code
  bool validateHotp(
    String secret,
    String code,
    int counter, {
    int digits = 6,
    int window = 10,
    Algorithm algorithm = Algorithm.SHA1,
  }) {
    try {
      // Check within counter window
      for (int i = 0; i <= window; i++) {
        final expectedCode = OTP.generateHOTPCodeString(
          secret,
          counter + i,
          length: digits,
          algorithm: algorithm,
          isGoogle: true,
        );
        
        if (expectedCode == code) {
          return true;
        }
      }
      
      return false;
    } catch (e) {
      print('Error validating HOTP: $e');
      return false;
    }
  }

  /// Generate a random secret key
  String generateSecret({int length = 20}) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'; // Base32 characters
    final random = Random.secure();
    final buffer = StringBuffer();
    
    for (int i = 0; i < length; i++) {
      final index = random.nextInt(chars.length);
      buffer.write(chars[index]);
    }
    
    return buffer.toString();
  }

  /// Generate provisioning URI for QR code
  String generateProvisioningUri({
    required String secret,
    required String issuer,
    required String account,
    bool isTotp = true,
    int digits = 6,
    int period = 30,
    Algorithm algorithm = Algorithm.SHA1,
  }) {
    final type = isTotp ? 'totp' : 'hotp';
    final encodedIssuer = Uri.encodeComponent(issuer);
    final encodedAccount = Uri.encodeComponent(account);
    final algorithmName = _getAlgorithmName(algorithm);
    
    if (isTotp) {
      return 'otpauth://$type/$encodedIssuer:$encodedAccount?secret=$secret&issuer=$encodedIssuer&algorithm=$algorithmName&digits=$digits&period=$period';
    } else {
      return 'otpauth://$type/$encodedIssuer:$encodedAccount?secret=$secret&issuer=$encodedIssuer&algorithm=$algorithmName&digits=$digits&counter=0';
    }
  }

  /// Get algorithm name string
  String _getAlgorithmName(Algorithm algorithm) {
    switch (algorithm) {
      case Algorithm.SHA1:
        return 'SHA1';
      case Algorithm.SHA256:
        return 'SHA256';
      case Algorithm.SHA512:
        return 'SHA512';
    }
  }

  /// Calculate remaining time for current TOTP code (in seconds)
  int getRemainingTime({int period = 30}) {
    final currentTime = DateTime.now().millisecondsSinceEpoch ~/ 1000;
    return period - (currentTime % period);
  }

  /// Hash a string using SHA256
  String hashString(String input) {
    final bytes = utf8.encode(input);
    final digest = sha256.convert(bytes);
    return digest.toString();
  }
}
