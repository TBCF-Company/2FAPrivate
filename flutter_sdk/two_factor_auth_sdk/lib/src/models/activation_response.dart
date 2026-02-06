/// Activation response model
class ActivationResponse {
  final bool success;
  final String message;
  final String? activationToken;
  final DateTime? activatedAt;

  ActivationResponse({
    required this.success,
    required this.message,
    this.activationToken,
    this.activatedAt,
  });

  Map<String, dynamic> toJson() {
    return {
      'success': success,
      'message': message,
      if (activationToken != null) 'activationToken': activationToken,
      if (activatedAt != null) 'activatedAt': activatedAt!.toIso8601String(),
    };
  }

  factory ActivationResponse.fromJson(Map<String, dynamic> json) {
    return ActivationResponse(
      success: json['success'] as bool,
      message: json['message'] as String,
      activationToken: json['activationToken'] as String?,
      activatedAt: json['activatedAt'] != null
          ? DateTime.parse(json['activatedAt'] as String)
          : null,
    );
  }
}
