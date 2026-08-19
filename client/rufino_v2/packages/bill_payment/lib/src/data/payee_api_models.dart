import '../domain/payee.dart';

/// Maps the payee read model into domain entities.
abstract final class PayeeMapper {
  /// Builds a [Payee] from the API's JSON.
  static Payee fromJson(Map<String, dynamic> json) {
    final policy = json['amountPolicy'] as Map<String, dynamic>? ?? const {};
    return Payee(
      id: json['id'] as String,
      legalName: json['legalName'] as String,
      taxId: json['taxId'] as String,
      taxIdKind: json['taxIdKind'] as String,
      aliases: (json['aliases'] as List<dynamic>? ?? const [])
          .map((e) => e as String)
          .toList(),
      acceptedBanks: (json['acceptedBanks'] as List<dynamic>? ?? const [])
          .map((e) => e as String)
          .toList(),
      amountPolicy: AmountPolicy(
        kind: policy['kind'] as String? ?? 'Unbounded',
        expectedAmount: (policy['expectedAmount'] as num?)?.toDouble(),
        tolerancePercent: (policy['tolerancePercent'] as num?)?.toDouble(),
        minAmount: (policy['minAmount'] as num?)?.toDouble(),
        maxAmount: (policy['maxAmount'] as num?)?.toDouble(),
        isConclusive: policy['isConclusive'] as bool? ?? false,
      ),
      isActive: json['isActive'] as bool? ?? true,
    );
  }
}

/// Maps one page of the payee list.
abstract final class PayeePageMapper {
  /// Builds a [PayeePage] from the API's JSON.
  static PayeePage fromJson(Map<String, dynamic> json) {
    return PayeePage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => PayeeMapper.fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }
}
