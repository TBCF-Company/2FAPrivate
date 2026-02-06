import 'dart:convert';
import 'package:otp/otp.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/token_data.dart';

class OtpService {
  static const String _tokensKey = 'tokens';

  /// Generate TOTP code
  String generateTotp(String secret, {int digits = 6, int period = 30}) {
    try {
      final code = OTP.generateTOTPCodeString(
        secret,
        DateTime.now().millisecondsSinceEpoch,
        length: digits,
        interval: period,
        algorithm: Algorithm.SHA1,
        isGoogle: true,
      );
      return code;
    } catch (e) {
      print('Error generating TOTP: $e');
      return '000000';
    }
  }

  /// Generate HOTP code
  String generateHotp(String secret, int counter, {int digits = 6}) {
    try {
      final code = OTP.generateHOTPCodeString(
        secret,
        counter,
        length: digits,
        algorithm: Algorithm.SHA1,
        isGoogle: true,
      );
      return code;
    } catch (e) {
      print('Error generating HOTP: $e');
      return '000000';
    }
  }

  /// Save token to local storage
  Future<void> saveToken(TokenData token) async {
    final prefs = await SharedPreferences.getInstance();
    final tokens = await getTokens();
    tokens.add(token);
    
    final jsonList = tokens.map((t) => t.toJson()).toList();
    await prefs.setString(_tokensKey, jsonEncode(jsonList));
  }

  /// Get all saved tokens
  Future<List<TokenData>> getTokens() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonString = prefs.getString(_tokensKey);
    
    if (jsonString == null || jsonString.isEmpty) {
      return [];
    }
    
    try {
      final jsonList = jsonDecode(jsonString) as List;
      return jsonList
          .map((json) => TokenData.fromJson(json as Map<String, dynamic>))
          .toList();
    } catch (e) {
      print('Error loading tokens: $e');
      return [];
    }
  }

  /// Delete a token
  Future<void> deleteToken(String account) async {
    final prefs = await SharedPreferences.getInstance();
    final tokens = await getTokens();
    tokens.removeWhere((t) => t.account == account);
    
    final jsonList = tokens.map((t) => t.toJson()).toList();
    await prefs.setString(_tokensKey, jsonEncode(jsonList));
  }

  /// Clear all tokens
  Future<void> clearAllTokens() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokensKey);
  }
}
