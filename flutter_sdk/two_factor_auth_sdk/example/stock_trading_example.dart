import 'package:flutter/material.dart';
import 'package:two_factor_auth_sdk/two_factor_auth_sdk.dart';

/// Stock Trading Transaction Authentication Example
/// 
/// This example demonstrates how to use the two_factor_auth_sdk
/// for authenticating stock trading transactions
void main() {
  runApp(const StockTradingApp());
}

class StockTradingApp extends StatelessWidget {
  const StockTradingApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Stock Trading 2FA Example',
      theme: ThemeData(
        primarySwatch: Colors.blue,
        useMaterial3: true,
      ),
      home: const DeviceCheckScreen(),
    );
  }
}

/// Screen to check device activation status
class DeviceCheckScreen extends StatefulWidget {
  const DeviceCheckScreen({Key? key}) : super(key: key);

  @override
  State<DeviceCheckScreen> createState() => _DeviceCheckScreenState();
}

class _DeviceCheckScreenState extends State<DeviceCheckScreen> {
  final _authManager = TransactionAuthManager(
    serverUrl: 'https://your-2fa-server.com', // Replace with your server URL
  );
  
  bool _isLoading = true;
  bool _isAuthorized = false;

  @override
  void initState() {
    super.initState();
    _checkDeviceAuthorization();
  }

  Future<void> _checkDeviceAuthorization() async {
    setState(() => _isLoading = true);
    
    final isAuthorized = await _authManager.isDeviceAuthorized();
    
    setState(() {
      _isAuthorized = isAuthorized;
      _isLoading = false;
    });

    if (isAuthorized) {
      // Navigate to trading screen
      if (mounted) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (context) => TradingScreen(authManager: _authManager),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (!_isAuthorized) {
      return DeviceActivationScreen(authManager: _authManager);
    }

    return const Scaffold(
      body: Center(child: CircularProgressIndicator()),
    );
  }
}

/// Device activation screen
class DeviceActivationScreen extends StatefulWidget {
  final TransactionAuthManager authManager;

  const DeviceActivationScreen({
    Key? key,
    required this.authManager,
  }) : super(key: key);

  @override
  State<DeviceActivationScreen> createState() => _DeviceActivationScreenState();
}

class _DeviceActivationScreenState extends State<DeviceActivationScreen> {
  final _otpController = TextEditingController();
  final _usernameController = TextEditingController();
  
  String? _secret;
  String? _displayOtp;
  bool _isLoading = false;
  String? _message;
  bool _activationRequested = false;

  Future<void> _requestActivation() async {
    if (_usernameController.text.isEmpty) {
      setState(() {
        _message = 'Vui lòng nhập tên đăng nhập';
      });
      return;
    }

    setState(() {
      _isLoading = true;
      _message = null;
    });

    try {
      final result = await widget.authManager.requestDeviceActivation(
        username: _usernameController.text,
        issuer: 'StockTrading',
      );

      setState(() {
        _secret = result['secret'];
        _displayOtp = result['otpCode'];
        _activationRequested = true;
        _message = 'Mã OTP đã được tạo. Hãy nhập mã OTP hiển thị trên giao diện web/admin.';
      });
    } catch (e) {
      setState(() {
        _message = 'Lỗi: $e';
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _activateDevice() async {
    if (_otpController.text.isEmpty) {
      setState(() {
        _message = 'Vui lòng nhập mã OTP';
      });
      return;
    }

    setState(() {
      _isLoading = true;
      _message = null;
    });

    try {
      final success = await widget.authManager.activateDevice(
        otpCode: _otpController.text,
        secret: _secret!,
        username: _usernameController.text,
        issuer: 'StockTrading',
      );

      if (success) {
        if (mounted) {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(
              builder: (context) => TradingScreen(authManager: widget.authManager),
            ),
          );
        }
      } else {
        setState(() {
          _message = 'Kích hoạt thất bại. Mã OTP không đúng.';
        });
      }
    } catch (e) {
      setState(() {
        _message = 'Lỗi: $e';
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Kích hoạt thiết bị'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(
              Icons.security,
              size: 80,
              color: Colors.blue,
            ),
            const SizedBox(height: 20),
            const Text(
              'Xác thực thiết bị',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 10),
            const Text(
              'Để đảm bảo an toàn giao dịch chứng khoán, bạn cần kích hoạt thiết bị này.',
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 30),
            
            if (!_activationRequested) ...[
              TextField(
                controller: _usernameController,
                decoration: const InputDecoration(
                  labelText: 'Tên đăng nhập',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.person),
                ),
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: _isLoading ? null : _requestActivation,
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.all(16),
                ),
                child: const Text('Yêu cầu kích hoạt'),
              ),
            ] else ...[
              Card(
                color: Colors.blue.shade50,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    children: [
                      const Text(
                        'Mã OTP hiển thị trên giao diện web/admin:',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 10),
                      Text(
                        _displayOtp ?? '',
                        style: const TextStyle(
                          fontSize: 32,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 4,
                        ),
                      ),
                      const SizedBox(height: 10),
                      const Text(
                        '(Trong môi trường thực tế, mã này sẽ hiển thị trên web/admin)',
                        style: TextStyle(fontSize: 12, fontStyle: FontStyle.italic),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 20),
              TextField(
                controller: _otpController,
                decoration: const InputDecoration(
                  labelText: 'Nhập mã OTP',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.lock),
                ),
                keyboardType: TextInputType.number,
                maxLength: 6,
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: _isLoading ? null : _activateDevice,
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.all(16),
                  backgroundColor: Colors.green,
                ),
                child: const Text('Kích hoạt thiết bị'),
              ),
            ],
            
            if (_message != null) ...[
              const SizedBox(height: 20),
              Card(
                color: _message!.contains('thất bại') || _message!.contains('Lỗi')
                    ? Colors.red.shade50
                    : Colors.blue.shade50,
                child: Padding(
                  padding: const EdgeInsets.all(12.0),
                  child: Text(
                    _message!,
                    style: TextStyle(
                      color: _message!.contains('thất bại') || _message!.contains('Lỗi')
                          ? Colors.red
                          : Colors.blue,
                    ),
                  ),
                ),
              ),
            ],
            
            if (_isLoading) ...[
              const SizedBox(height: 20),
              const Center(child: CircularProgressIndicator()),
            ],
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _otpController.dispose();
    _usernameController.dispose();
    super.dispose();
  }
}

/// Trading screen with transaction authentication
class TradingScreen extends StatefulWidget {
  final TransactionAuthManager authManager;

  const TradingScreen({
    Key? key,
    required this.authManager,
  }) : super(key: key);

  @override
  State<TradingScreen> createState() => _TradingScreenState();
}

class _TradingScreenState extends State<TradingScreen> {
  final _amountController = TextEditingController();
  final _stockCodeController = TextEditingController();
  
  // ⚠️ WARNING: In production, NEVER hardcode the secret in the app!
  // The secret should be retrieved securely from your backend server after authentication.
  // This is a placeholder for demonstration purposes only.
  // Example: final _userSecret = await yourApi.getUserSecret();
  final String _userSecret = 'JBSWY3DPEHPK3PXP'; // ⚠️ EXAMPLE ONLY - DO NOT USE IN PRODUCTION!
  
  String? _currentOtp;
  int _remainingTime = 30;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _generateOtp();
    _startOtpTimer();
  }

  Future<void> _generateOtp() async {
    final otp = await widget.authManager.generateTransactionOTP(_userSecret);
    setState(() {
      _currentOtp = otp;
    });
  }

  void _startOtpTimer() {
    Future.delayed(const Duration(seconds: 1), () {
      if (mounted) {
        final remaining = widget.authManager.getOtpRemainingTime();
        setState(() {
          _remainingTime = remaining;
        });
        
        if (remaining <= 1) {
          _generateOtp();
        }
        
        _startOtpTimer();
      }
    });
  }

  Future<void> _executeTrade(String transactionType) async {
    if (_stockCodeController.text.isEmpty || _amountController.text.isEmpty) {
      _showMessage('Vui lòng nhập đầy đủ thông tin');
      return;
    }

    final amount = double.tryParse(_amountController.text);
    if (amount == null || amount <= 0) {
      _showMessage('Số lượng không hợp lệ');
      return;
    }

    setState(() => _isLoading = true);

    try {
      // Create transaction signature
      final signature = await widget.authManager.createTransactionSignature(
        secret: _userSecret,
        transactionId: 'TXN_${DateTime.now().millisecondsSinceEpoch}',
        transactionType: transactionType,
        amount: amount,
      );

      // In production, send this signature to your server for verification
      // The server will validate the OTP and execute the trade
      
      // Simulate server processing
      await Future.delayed(const Duration(seconds: 1));
      
      if (mounted) {
        _showSuccessDialog(transactionType, signature);
      }
    } catch (e) {
      _showMessage('Lỗi: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }

  void _showSuccessDialog(String type, Map<String, String> signature) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Giao dịch thành công'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Loại: $type'),
            Text('Mã CP: ${_stockCodeController.text}'),
            Text('Số lượng: ${_amountController.text}'),
            const Divider(),
            const Text('Chữ ký giao dịch:', style: TextStyle(fontWeight: FontWeight.bold)),
            Text('OTP: ${signature['otpCode']}', style: const TextStyle(fontSize: 12)),
            Text('Device ID: ${signature['deviceId']}', style: const TextStyle(fontSize: 12)),
            Text('Transaction ID: ${signature['transactionId']}', style: const TextStyle(fontSize: 12)),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              _stockCodeController.clear();
              _amountController.clear();
            },
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Giao dịch chứng khoán'),
        actions: [
          IconButton(
            icon: const Icon(Icons.info_outline),
            onPressed: () => _showDeviceInfo(),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // OTP Display Card
            Card(
              elevation: 4,
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  children: [
                    const Text(
                      'Mã xác thực giao dịch (OTP)',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      _currentOtp ?? '------',
                      style: const TextStyle(
                        fontSize: 36,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 8,
                        fontFamily: 'monospace',
                      ),
                    ),
                    const SizedBox(height: 10),
                    LinearProgressIndicator(
                      value: _remainingTime / 30,
                      backgroundColor: Colors.grey.shade300,
                      valueColor: AlwaysStoppedAnimation<Color>(
                        _remainingTime < 10 ? Colors.red : Colors.green,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      'Còn lại: $_remainingTime giây',
                      style: TextStyle(
                        color: _remainingTime < 10 ? Colors.red : Colors.grey,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            
            const SizedBox(height: 30),
            
            // Trading Form
            const Text(
              'Thông tin giao dịch',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 15),
            
            TextField(
              controller: _stockCodeController,
              decoration: const InputDecoration(
                labelText: 'Mã chứng khoán',
                hintText: 'VD: VNM, FPT, HPG',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.trending_up),
              ),
              textCapitalization: TextCapitalization.characters,
            ),
            
            const SizedBox(height: 15),
            
            TextField(
              controller: _amountController,
              decoration: const InputDecoration(
                labelText: 'Số lượng',
                hintText: 'Nhập số lượng cổ phiếu',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.numbers),
              ),
              keyboardType: TextInputType.number,
            ),
            
            const SizedBox(height: 25),
            
            // Action Buttons
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: _isLoading ? null : () => _executeTrade('MUA'),
                    icon: const Icon(Icons.add_shopping_cart),
                    label: const Text('MUA'),
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.all(16),
                      backgroundColor: Colors.green,
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: _isLoading ? null : () => _executeTrade('BÁN'),
                    icon: const Icon(Icons.remove_shopping_cart),
                    label: const Text('BÁN'),
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.all(16),
                      backgroundColor: Colors.red,
                    ),
                  ),
                ),
              ],
            ),
            
            if (_isLoading) ...[
              const SizedBox(height: 20),
              const Center(child: CircularProgressIndicator()),
            ],
            
            const SizedBox(height: 30),
            
            const Card(
              color: Color(0xFFFFF3E0),
              child: Padding(
                padding: EdgeInsets.all(12.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.info, color: Colors.orange),
                        SizedBox(width: 10),
                        Text(
                          'Lưu ý bảo mật',
                          style: TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    SizedBox(height: 8),
                    Text(
                      '• Mã OTP thay đổi mỗi 30 giây\n'
                      '• Mỗi giao dịch được xác thực với thiết bị đã đăng ký\n'
                      '• Không chia sẻ mã OTP với bất kỳ ai',
                      style: TextStyle(fontSize: 13),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showDeviceInfo() async {
    final deviceInfo = await widget.authManager.getDeviceInfo();
    
    if (mounted) {
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Thông tin thiết bị'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Tên: ${deviceInfo.deviceName}'),
              Text('ID: ${deviceInfo.deviceId}'),
              Text('Nền tảng: ${deviceInfo.platform}'),
              Text('Model: ${deviceInfo.model}'),
              Text('Phiên bản OS: ${deviceInfo.osVersion}'),
              Text('Đã kích hoạt: ${deviceInfo.isActivated ? "Có" : "Không"}'),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Đóng'),
            ),
          ],
        ),
      );
    }
  }

  @override
  void dispose() {
    _amountController.dispose();
    _stockCodeController.dispose();
    super.dispose();
  }
}
