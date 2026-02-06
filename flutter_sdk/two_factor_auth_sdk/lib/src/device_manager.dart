import 'dart:io';
import 'package:device_info_plus/device_info_plus.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'dart:convert';
import 'models/device_info.dart';

/// Device manager for identifying and managing devices
class DeviceManager {
  static const String _deviceIdKey = 'device_id';
  static const String _deviceInfoKey = 'device_info';
  static const String _whitelistedDevicesKey = 'whitelisted_devices';

  final DeviceInfoPlugin _deviceInfo = DeviceInfoPlugin();

  /// Get the unique device ID
  /// For Android: uses androidId
  /// For iOS: uses identifierForVendor
  Future<String> getDeviceId() async {
    final prefs = await SharedPreferences.getInstance();
    
    // Check if we already have a stored device ID
    String? storedId = prefs.getString(_deviceIdKey);
    if (storedId != null && storedId.isNotEmpty) {
      return storedId;
    }

    // Generate or get device ID based on platform
    String deviceId;
    
    if (Platform.isAndroid) {
      final androidInfo = await _deviceInfo.androidInfo;
      deviceId = androidInfo.id; // androidId
    } else if (Platform.isIOS) {
      final iosInfo = await _deviceInfo.iosInfo;
      deviceId = iosInfo.identifierForVendor ?? _generateFallbackId();
    } else {
      deviceId = _generateFallbackId();
    }

    // Store the device ID for future use
    await prefs.setString(_deviceIdKey, deviceId);
    return deviceId;
  }

  /// Get comprehensive device information
  Future<DeviceInfo> getDeviceInfo() async {
    final deviceId = await getDeviceId();
    
    if (Platform.isAndroid) {
      final androidInfo = await _deviceInfo.androidInfo;
      return DeviceInfo(
        deviceId: deviceId,
        deviceName: androidInfo.model,
        platform: 'Android',
        osVersion: androidInfo.version.release,
        model: '${androidInfo.manufacturer} ${androidInfo.model}',
        registeredAt: DateTime.now(),
      );
    } else if (Platform.isIOS) {
      final iosInfo = await _deviceInfo.iosInfo;
      return DeviceInfo(
        deviceId: deviceId,
        deviceName: iosInfo.name,
        platform: 'iOS',
        osVersion: iosInfo.systemVersion,
        model: iosInfo.utsname.machine,
        registeredAt: DateTime.now(),
      );
    } else {
      return DeviceInfo(
        deviceId: deviceId,
        deviceName: 'Unknown',
        platform: Platform.operatingSystem,
        osVersion: Platform.operatingSystemVersion,
        model: 'Unknown',
        registeredAt: DateTime.now(),
      );
    }
  }

  /// Save current device info to local storage
  Future<void> saveCurrentDeviceInfo(DeviceInfo deviceInfo) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_deviceInfoKey, jsonEncode(deviceInfo.toJson()));
  }

  /// Get saved device info from local storage
  Future<DeviceInfo?> getSavedDeviceInfo() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonString = prefs.getString(_deviceInfoKey);
    
    if (jsonString == null || jsonString.isEmpty) {
      return null;
    }
    
    try {
      final json = jsonDecode(jsonString) as Map<String, dynamic>;
      return DeviceInfo.fromJson(json);
    } catch (e) {
      print('Error loading device info: $e');
      return null;
    }
  }

  /// Check if current device is whitelisted
  Future<bool> isCurrentDeviceWhitelisted() async {
    final deviceId = await getDeviceId();
    return await isDeviceWhitelisted(deviceId);
  }

  /// Check if a specific device is whitelisted
  Future<bool> isDeviceWhitelisted(String deviceId) async {
    final whitelistedDevices = await getWhitelistedDevices();
    return whitelistedDevices.any((device) => device.deviceId == deviceId);
  }

  /// Add device to whitelist
  Future<void> addToWhitelist(DeviceInfo deviceInfo) async {
    final prefs = await SharedPreferences.getInstance();
    final whitelistedDevices = await getWhitelistedDevices();
    
    // Remove existing entry if present
    whitelistedDevices.removeWhere((d) => d.deviceId == deviceInfo.deviceId);
    
    // Add new entry
    whitelistedDevices.add(deviceInfo);
    
    // Save to storage
    final jsonList = whitelistedDevices.map((d) => d.toJson()).toList();
    await prefs.setString(_whitelistedDevicesKey, jsonEncode(jsonList));
  }

  /// Get all whitelisted devices
  Future<List<DeviceInfo>> getWhitelistedDevices() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonString = prefs.getString(_whitelistedDevicesKey);
    
    if (jsonString == null || jsonString.isEmpty) {
      return [];
    }
    
    try {
      final jsonList = jsonDecode(jsonString) as List;
      return jsonList
          .map((json) => DeviceInfo.fromJson(json as Map<String, dynamic>))
          .toList();
    } catch (e) {
      print('Error loading whitelisted devices: $e');
      return [];
    }
  }

  /// Remove device from whitelist
  Future<void> removeFromWhitelist(String deviceId) async {
    final prefs = await SharedPreferences.getInstance();
    final whitelistedDevices = await getWhitelistedDevices();
    
    whitelistedDevices.removeWhere((d) => d.deviceId == deviceId);
    
    final jsonList = whitelistedDevices.map((d) => d.toJson()).toList();
    await prefs.setString(_whitelistedDevicesKey, jsonEncode(jsonList));
  }

  /// Clear all whitelisted devices
  Future<void> clearWhitelist() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_whitelistedDevicesKey);
  }

  /// Mark current device as activated
  Future<void> markCurrentDeviceActivated() async {
    final deviceInfo = await getSavedDeviceInfo();
    if (deviceInfo != null) {
      final activatedDeviceInfo = deviceInfo.copyWith(isActivated: true);
      await saveCurrentDeviceInfo(activatedDeviceInfo);
      await addToWhitelist(activatedDeviceInfo);
    }
  }

  /// Check if current device is activated
  Future<bool> isCurrentDeviceActivated() async {
    final deviceInfo = await getSavedDeviceInfo();
    return deviceInfo?.isActivated ?? false;
  }

  /// Generate a fallback device ID using timestamp and random string
  String _generateFallbackId() {
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    return 'device_${timestamp}_${_randomString(8)}';
  }

  String _randomString(int length) {
    const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
    return List.generate(
      length,
      (index) => chars[(DateTime.now().microsecond + index) % chars.length],
    ).join();
  }
}
