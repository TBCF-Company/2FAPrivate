import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../services/otp_service.dart';
import '../models/token_data.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final OtpService _otpService = OtpService();
  List<TokenData> _tokens = [];

  @override
  void initState() {
    super.initState();
    _loadTokens();
  }

  Future<void> _loadTokens() async {
    final tokens = await _otpService.getTokens();
    setState(() {
      _tokens = tokens;
    });
  }

  Future<void> _addToken() async {
    // Navigate to QR scanner
    final result = await Navigator.pushNamed(context, '/qr-scanner');
    if (result != null && result is TokenData) {
      await _otpService.saveToken(result);
      _loadTokens();
    }
  }

  Future<void> _showOtpDisplay(TokenData token) async {
    Navigator.pushNamed(
      context,
      '/otp-display',
      arguments: token,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('2FA Tokens'),
        elevation: 2,
      ),
      body: _tokens.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.security,
                    size: 100,
                    color: Colors.grey[300],
                  ),
                  const SizedBox(height: 20),
                  Text(
                    'No tokens added yet',
                    style: TextStyle(
                      fontSize: 18,
                      color: Colors.grey[600],
                    ),
                  ),
                  const SizedBox(height: 10),
                  Text(
                    'Tap + to scan a QR code',
                    style: TextStyle(
                      color: Colors.grey[500],
                    ),
                  ),
                ],
              ),
            )
          : ListView.builder(
              itemCount: _tokens.length,
              padding: const EdgeInsets.all(16),
              itemBuilder: (context, index) {
                final token = _tokens[index];
                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: ListTile(
                    leading: CircleAvatar(
                      backgroundColor: Colors.blue[100],
                      child: Icon(
                        Icons.lock,
                        color: Colors.blue[700],
                      ),
                    ),
                    title: Text(
                      token.account,
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    subtitle: Text(token.issuer),
                    trailing: Icon(Icons.arrow_forward_ios),
                    onTap: () => _showOtpDisplay(token),
                  ),
                );
              },
            ),
      floatingActionButton: FloatingActionButton(
        onPressed: _addToken,
        child: const Icon(Icons.add),
        tooltip: 'Add Token',
      ),
    );
  }
}
