/// Activation request model
class ActivationRequest {
  final String deviceId;
  final String otpCode;
  final String? username;
  final String? issuer;

  ActivationRequest({
    required this.deviceId,
    required this.otpCode,
    this.username,
    this.issuer,
  });

  Map<String, dynamic> toJson() {
    return {
      'deviceId': deviceId,
      'otpCode': otpCode,
      if (username != null) 'username': username,
      if (issuer != null) 'issuer': issuer,
    };
  }

  factory ActivationRequest.fromJson(Map<String, dynamic> json) {
    return ActivationRequest(
      deviceId: json['deviceId'] as String,
      otpCode: json['otpCode'] as String,
      username: json['username'] as String?,
      issuer: json['issuer'] as String?,
    );
  }
}
