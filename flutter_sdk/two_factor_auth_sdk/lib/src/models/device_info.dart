/// Device information model
class DeviceInfo {
  final String deviceId;
  final String deviceName;
  final String platform;
  final String osVersion;
  final String model;
  final DateTime registeredAt;
  final bool isActivated;

  DeviceInfo({
    required this.deviceId,
    required this.deviceName,
    required this.platform,
    required this.osVersion,
    required this.model,
    required this.registeredAt,
    this.isActivated = false,
  });

  Map<String, dynamic> toJson() {
    return {
      'deviceId': deviceId,
      'deviceName': deviceName,
      'platform': platform,
      'osVersion': osVersion,
      'model': model,
      'registeredAt': registeredAt.toIso8601String(),
      'isActivated': isActivated,
    };
  }

  factory DeviceInfo.fromJson(Map<String, dynamic> json) {
    return DeviceInfo(
      deviceId: json['deviceId'] as String,
      deviceName: json['deviceName'] as String,
      platform: json['platform'] as String,
      osVersion: json['osVersion'] as String,
      model: json['model'] as String,
      registeredAt: DateTime.parse(json['registeredAt'] as String),
      isActivated: json['isActivated'] as bool? ?? false,
    );
  }

  DeviceInfo copyWith({
    String? deviceId,
    String? deviceName,
    String? platform,
    String? osVersion,
    String? model,
    DateTime? registeredAt,
    bool? isActivated,
  }) {
    return DeviceInfo(
      deviceId: deviceId ?? this.deviceId,
      deviceName: deviceName ?? this.deviceName,
      platform: platform ?? this.platform,
      osVersion: osVersion ?? this.osVersion,
      model: model ?? this.model,
      registeredAt: registeredAt ?? this.registeredAt,
      isActivated: isActivated ?? this.isActivated,
    );
  }
}
