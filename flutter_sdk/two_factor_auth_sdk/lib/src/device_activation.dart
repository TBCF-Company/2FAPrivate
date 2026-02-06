import 'dart:convert';
import 'package:http/http.dart' as http;
import 'device_manager.dart';
import 'otp_manager.dart';
import 'models/device_info.dart';
import 'models/activation_request.dart';
import 'models/activation_response.dart';

/// Device activation manager for handling OTP-based device activation
class DeviceActivation {
  final DeviceManager _deviceManager;
  final OtpManager _otpManager;
  final String? baseUrl;

  DeviceActivation({
    DeviceManager? deviceManager,
    OtpManager? otpManager,
    this.baseUrl,
  })  : _deviceManager = deviceManager ?? DeviceManager(),
        _otpManager = otpManager ?? OtpManager();

  /// Request device activation from server
  /// Returns the OTP secret that needs to be entered on the device
  Future<Map<String, dynamic>> requestActivation({
    required String username,
    required String issuer,
  }) async {
    try {
      final deviceInfo = await _deviceManager.getDeviceInfo();
      
      // If baseUrl is provided, make API call to server
      if (baseUrl != null && baseUrl!.isNotEmpty) {
        final url = Uri.parse('$baseUrl/api/device/request-activation');
        final response = await http.post(
          url,
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({
            'deviceId': deviceInfo.deviceId,
            'deviceName': deviceInfo.deviceName,
            'platform': deviceInfo.platform,
            'osVersion': deviceInfo.osVersion,
            'model': deviceInfo.model,
            'username': username,
            'issuer': issuer,
          }),
        );

        if (response.statusCode == 200) {
          final data = jsonDecode(response.body) as Map<String, dynamic>;
          return data;
        } else {
          throw Exception('Failed to request activation: ${response.statusCode}');
        }
      } else {
        // Local mode: generate secret directly
        final secret = _otpManager.generateSecret();
        final otpCode = _otpManager.generateTotp(secret);
        
        return {
          'success': true,
          'deviceId': deviceInfo.deviceId,
          'secret': secret,
          'otpCode': otpCode,
          'message': 'Activation requested. Enter the OTP code shown on the web interface.',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'Error requesting activation: $e',
      };
    }
  }

  /// Activate device with OTP code
  /// The OTP code should be displayed on the web/admin interface
  /// and entered here by the user
  Future<ActivationResponse> activateDevice({
    required String otpCode,
    String? secret,
    String? username,
    String? issuer,
  }) async {
    try {
      final deviceInfo = await _deviceManager.getDeviceInfo();
      final deviceId = deviceInfo.deviceId;

      // If baseUrl is provided, make API call to server
      if (baseUrl != null && baseUrl!.isNotEmpty) {
        final url = Uri.parse('$baseUrl/api/device/activate');
        final request = ActivationRequest(
          deviceId: deviceId,
          otpCode: otpCode,
          username: username,
          issuer: issuer,
        );

        final response = await http.post(
          url,
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode(request.toJson()),
        );

        if (response.statusCode == 200) {
          final data = jsonDecode(response.body) as Map<String, dynamic>;
          final activationResponse = ActivationResponse.fromJson(data);

          if (activationResponse.success) {
            // Mark device as activated locally
            await _deviceManager.markCurrentDeviceActivated();
          }

          return activationResponse;
        } else {
          return ActivationResponse(
            success: false,
            message: 'Activation failed: ${response.statusCode}',
          );
        }
      } else {
        // Local mode: validate OTP directly
        if (secret == null || secret.isEmpty) {
          return ActivationResponse(
            success: false,
            message: 'Secret is required for local activation',
          );
        }

        final isValid = _otpManager.validateTotp(secret, otpCode);

        if (isValid) {
          // Mark device as activated
          final activatedDeviceInfo = deviceInfo.copyWith(isActivated: true);
          await _deviceManager.saveCurrentDeviceInfo(activatedDeviceInfo);
          await _deviceManager.addToWhitelist(activatedDeviceInfo);

          return ActivationResponse(
            success: true,
            message: 'Device activated successfully',
            activatedAt: DateTime.now(),
          );
        } else {
          return ActivationResponse(
            success: false,
            message: 'Invalid OTP code',
          );
        }
      }
    } catch (e) {
      return ActivationResponse(
        success: false,
        message: 'Error activating device: $e',
      );
    }
  }

  /// Check if current device is activated
  Future<bool> isDeviceActivated() async {
    return await _deviceManager.isCurrentDeviceActivated();
  }

  /// Get current device information
  Future<DeviceInfo> getDeviceInfo() async {
    return await _deviceManager.getDeviceInfo();
  }

  /// Deactivate current device
  Future<void> deactivateDevice() async {
    final deviceInfo = await _deviceManager.getSavedDeviceInfo();
    if (deviceInfo != null) {
      final deactivatedDeviceInfo = deviceInfo.copyWith(isActivated: false);
      await _deviceManager.saveCurrentDeviceInfo(deactivatedDeviceInfo);
      await _deviceManager.removeFromWhitelist(deviceInfo.deviceId);
    }
  }

  /// Verify device is whitelisted before allowing access
  Future<bool> verifyDeviceAccess() async {
    final isActivated = await _deviceManager.isCurrentDeviceActivated();
    final isWhitelisted = await _deviceManager.isCurrentDeviceWhitelisted();
    return isActivated && isWhitelisted;
  }
}
