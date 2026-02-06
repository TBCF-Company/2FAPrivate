class TokenData {
  final String secret;
  final String issuer;
  final String account;
  final bool isTotp;
  final int digits;
  final int period;

  TokenData({
    required this.secret,
    required this.issuer,
    required this.account,
    this.isTotp = true,
    this.digits = 6,
    this.period = 30,
  });

  Map<String, dynamic> toJson() {
    return {
      'secret': secret,
      'issuer': issuer,
      'account': account,
      'isTotp': isTotp,
      'digits': digits,
      'period': period,
    };
  }

  factory TokenData.fromJson(Map<String, dynamic> json) {
    return TokenData(
      secret: json['secret'] as String,
      issuer: json['issuer'] as String,
      account: json['account'] as String,
      isTotp: json['isTotp'] as bool? ?? true,
      digits: json['digits'] as int? ?? 6,
      period: json['period'] as int? ?? 30,
    );
  }
}
