import '../domain/payer_profile.dart';

/// Maps the payer profile read model into the domain entity.
abstract final class PayerProfileMapper {
  /// Builds a [PayerProfile] from the API's JSON.
  static PayerProfile fromJson(Map<String, dynamic> json) {
    return PayerProfile(
      id: json['id'] as String,
      kind: json['kind'] as String,
      legalName: json['legalName'] as String,
      primaryTaxId: json['primaryTaxId'] as String,
      primaryTaxIdKind: json['primaryTaxIdKind'] as String,
      additionalTaxIds:
          (json['additionalTaxIds'] as List<dynamic>? ?? const [])
              .map((e) => e as Map<String, dynamic>)
              .map(
                (e) => PayerTaxId(
                  value: e['value'] as String,
                  kind: e['kind'] as String,
                ),
              )
              .toList(),
      matchByCnpjRoot: json['matchByCnpjRoot'] as bool? ?? false,
      canSchedulePayments: json['canSchedulePayments'] as bool? ?? false,
    );
  }
}
