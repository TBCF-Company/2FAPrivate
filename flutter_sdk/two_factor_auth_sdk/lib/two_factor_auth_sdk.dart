/// A Flutter SDK for two-factor authentication with device management.
///
/// This library provides:
/// - Device identification for Android and iOS
/// - Device registration and whitelist management
/// - OTP generation and validation
/// - Device activation with OTP verification
/// - Transaction authentication for stock trading and financial apps
library two_factor_auth_sdk;

export 'src/device_manager.dart';
export 'src/otp_manager.dart';
export 'src/device_activation.dart';
export 'src/transaction_auth_manager.dart';
export 'src/models/device_info.dart';
export 'src/models/activation_request.dart';
export 'src/models/activation_response.dart';
