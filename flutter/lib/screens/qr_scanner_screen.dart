import 'package:flutter/material.dart';
import '../models/token_data.dart';

class QrScannerScreen extends StatefulWidget {
  const QrScannerScreen({super.key});

  @override
  State<QrScannerScreen> createState() => _QrScannerScreenState();
}

class _QrScannerScreenState extends State<QrScannerScreen> {
  final TextEditingController _secretController = TextEditingController();
  final TextEditingController _issuerController = TextEditingController();
  final TextEditingController _accountController = TextEditingController();

  void _saveToken() {
    if (_secretController.text.isEmpty ||
        _accountController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please fill in all required fields'),
        ),
      );
      return;
    }

    final token = TokenData(
      secret: _secretController.text,
      issuer: _issuerController.text.isEmpty ? '2FA App' : _issuerController.text,
      account: _accountController.text,
    );

    Navigator.pop(context, token);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Add Token'),
        elevation: 2,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(
              Icons.qr_code_scanner,
              size: 100,
              color: Colors.blue,
            ),
            const SizedBox(height: 20),
            
            const Text(
              'Manual Entry',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
              ),
              textAlign: TextAlign.center,
            ),
            
            const SizedBox(height: 10),
            
            Text(
              'Enter your token details manually',
              style: TextStyle(
                color: Colors.grey[600],
              ),
              textAlign: TextAlign.center,
            ),
            
            const SizedBox(height: 40),
            
            TextField(
              controller: _accountController,
              decoration: const InputDecoration(
                labelText: 'Account *',
                hintText: 'user@example.com',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.person),
              ),
            ),
            
            const SizedBox(height: 16),
            
            TextField(
              controller: _issuerController,
              decoration: const InputDecoration(
                labelText: 'Issuer',
                hintText: '2FA Demo App',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.business),
              ),
            ),
            
            const SizedBox(height: 16),
            
            TextField(
              controller: _secretController,
              decoration: const InputDecoration(
                labelText: 'Secret Key *',
                hintText: 'JBSWY3DPEHPK3PXP',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.key),
              ),
              maxLines: 2,
            ),
            
            const SizedBox(height: 30),
            
            ElevatedButton(
              onPressed: _saveToken,
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
              child: const Text(
                'Add Token',
                style: TextStyle(fontSize: 16),
              ),
            ),
            
            const SizedBox(height: 20),
            
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.blue[50],
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Note:',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'In a production app, you would scan a QR code here. '
                    'For this demo, enter the secret key from the web app.',
                    style: TextStyle(
                      color: Colors.grey[700],
                      fontSize: 14,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _secretController.dispose();
    _issuerController.dispose();
    _accountController.dispose();
    super.dispose();
  }
}
