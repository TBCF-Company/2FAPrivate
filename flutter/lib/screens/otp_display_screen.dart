import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../models/token_data.dart';
import '../services/otp_service.dart';

class OtpDisplayScreen extends StatefulWidget {
  const OtpDisplayScreen({super.key});

  @override
  State<OtpDisplayScreen> createState() => _OtpDisplayScreenState();
}

class _OtpDisplayScreenState extends State<OtpDisplayScreen> {
  final OtpService _otpService = OtpService();
  String _currentOtp = '';
  int _timeRemaining = 30;
  Timer? _timer;
  TokenData? _token;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _token = ModalRoute.of(context)!.settings.arguments as TokenData?;
    if (_token != null) {
      _generateOtp();
      _startTimer();
    }
  }

  void _generateOtp() {
    if (_token != null) {
      final otp = _otpService.generateTotp(_token!.secret);
      setState(() {
        _currentOtp = otp;
      });
    }
  }

  void _startTimer() {
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      final now = DateTime.now();
      final secondsElapsed = now.second % 30;
      final remaining = 30 - secondsElapsed;
      
      setState(() {
        _timeRemaining = remaining;
      });
      
      if (remaining == 30) {
        _generateOtp();
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  void _copyOtp() {
    Clipboard.setData(ClipboardData(text: _currentOtp));
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('OTP code copied to clipboard'),
        duration: Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_token == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('OTP Display')),
        body: const Center(child: Text('No token data')),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text(_token!.account),
        elevation: 2,
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                _token!.issuer,
                style: TextStyle(
                  fontSize: 20,
                  color: Colors.grey[700],
                ),
              ),
              const SizedBox(height: 40),
              
              // OTP Code Display
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 40,
                  vertical: 30,
                ),
                decoration: BoxDecoration(
                  color: Colors.blue[50],
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: Colors.blue[200]!,
                    width: 2,
                  ),
                ),
                child: Text(
                  _formatOtp(_currentOtp),
                  style: const TextStyle(
                    fontSize: 48,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 8,
                    fontFamily: 'monospace',
                  ),
                ),
              ),
              
              const SizedBox(height: 30),
              
              // Timer
              Stack(
                alignment: Alignment.center,
                children: [
                  SizedBox(
                    width: 80,
                    height: 80,
                    child: CircularProgressIndicator(
                      value: _timeRemaining / 30,
                      strokeWidth: 6,
                      backgroundColor: Colors.grey[200],
                      valueColor: AlwaysStoppedAnimation<Color>(
                        _timeRemaining < 10 ? Colors.red : Colors.blue,
                      ),
                    ),
                  ),
                  Text(
                    '$_timeRemaining',
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: _timeRemaining < 10 ? Colors.red : Colors.blue,
                    ),
                  ),
                ],
              ),
              
              const SizedBox(height: 40),
              
              // Copy Button
              ElevatedButton.icon(
                onPressed: _copyOtp,
                icon: const Icon(Icons.copy),
                label: const Text('Copy Code'),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 32,
                    vertical: 16,
                  ),
                ),
              ),
              
              const SizedBox(height: 20),
              
              Text(
                'This code expires in ${_timeRemaining}s',
                style: TextStyle(
                  color: Colors.grey[600],
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _formatOtp(String otp) {
    if (otp.length != 6) return otp;
    return '${otp.substring(0, 3)} ${otp.substring(3)}';
  }
}
