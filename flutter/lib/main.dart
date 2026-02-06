import 'package:flutter/material.dart';
import 'screens/home_screen.dart';
import 'screens/otp_display_screen.dart';
import 'screens/qr_scanner_screen.dart';

void main() {
  runApp(const TwoFactorAuthApp());
}

class TwoFactorAuthApp extends StatelessWidget {
  const TwoFactorAuthApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: '2FA Mobile App',
      theme: ThemeData(
        primarySwatch: Colors.blue,
        useMaterial3: true,
      ),
      initialRoute: '/',
      routes: {
        '/': (context) => const HomeScreen(),
        '/otp-display': (context) => const OtpDisplayScreen(),
        '/qr-scanner': (context) => const QrScannerScreen(),
      },
    );
  }
}
