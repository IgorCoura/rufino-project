import '../domain/my_tenant.dart';
import '../domain/tenant.dart';
import '../domain/tenant_summary.dart';

/// Parses the API's date strings, tolerating a missing value.
DateTime _date(Object? value) =>
    DateTime.tryParse(value as String? ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);

List<String> _strings(Object? value) =>
    (value as List<dynamic>? ?? const []).map((e) => e as String).toList();

/// Maps `GET /api/v1/me/tenants` into [MyTenant].
abstract final class MyTenantMapper {
  /// Builds one entry.
  static MyTenant fromJson(Map<String, dynamic> json) {
    return MyTenant(
      id: json['id'] as String,
      kind: json['kind'] as String? ?? '',
      legalName: json['legalName'] as String? ?? '',
      tradeName: json['tradeName'] as String? ?? '',
      status: json['status'] as String? ?? '',
      role: json['role'] as String? ?? '',
      activeProducts: _strings(json['activeProducts']),
    );
  }
}

/// Maps the listing payload into [TenantPage].
abstract final class TenantPageMapper {
  /// Builds a page from `{items, nextCursor}`.
  static TenantPage fromJson(Map<String, dynamic> json) {
    final items = (json['items'] as List<dynamic>? ?? const [])
        .map((e) => TenantSummaryMapper.fromJson(e as Map<String, dynamic>))
        .toList();
    return TenantPage(
      items: items,
      nextCursor: json['nextCursor'] as String?,
    );
  }
}

/// Maps a listing row into [TenantSummary].
abstract final class TenantSummaryMapper {
  /// Builds one row.
  static TenantSummary fromJson(Map<String, dynamic> json) {
    return TenantSummary(
      id: json['id'] as String,
      kind: json['kind'] as String? ?? '',
      legalName: json['legalName'] as String? ?? '',
      tradeName: json['tradeName'] as String? ?? '',
      primaryTaxId: json['primaryTaxId'] as String? ?? '',
      status: json['status'] as String? ?? '',
      accessProvisioning: json['accessProvisioning'] as String? ?? '',
      contactEmail: json['contactEmail'] as String? ?? '',
      activeProducts: _strings(json['activeProducts']),
      createdAt: _date(json['createdAt']),
    );
  }
}

/// Maps the full cadastro into [Tenant].
abstract final class TenantMapper {
  /// Builds the aggregate as the read model returns it.
  static Tenant fromJson(Map<String, dynamic> json) {
    final contact = json['contact'] as Map<String, dynamic>? ?? const {};
    final address = json['address'] as Map<String, dynamic>? ?? const {};

    return Tenant(
      id: json['id'] as String,
      kind: json['kind'] as String? ?? '',
      legalName: json['legalName'] as String? ?? '',
      tradeName: json['tradeName'] as String? ?? '',
      primaryTaxId: json['primaryTaxId'] as String? ?? '',
      status: json['status'] as String? ?? '',
      suspensionReason: json['suspensionReason'] as String? ?? '',
      accessProvisioning: json['accessProvisioning'] as String? ?? '',
      contact: TenantContact(
        email: contact['email'] as String? ?? '',
        phone: contact['phone'] as String? ?? '',
      ),
      address: TenantAddress(
        zipCode: address['zipCode'] as String? ?? '',
        street: address['street'] as String? ?? '',
        number: address['number'] as String? ?? '',
        complement: address['complement'] as String? ?? '',
        neighborhood: address['neighborhood'] as String? ?? '',
        city: address['city'] as String? ?? '',
        state: address['state'] as String? ?? '',
        country: address['country'] as String? ?? 'BRASIL',
      ),
      products: (json['products'] as List<dynamic>? ?? const [])
          .map((e) => e as Map<String, dynamic>)
          .map(
            (p) => TenantProductInfo(
              product: p['product'] as String? ?? '',
              isActive: p['isActive'] as bool? ?? false,
              activatedAt: _date(p['activatedAt']),
              deactivatedAt: p['deactivatedAt'] == null
                  ? null
                  : _date(p['deactivatedAt']),
            ),
          )
          .toList(),
      memberships: (json['memberships'] as List<dynamic>? ?? const [])
          .map((e) => e as Map<String, dynamic>)
          .map(
            (m) => TenantMembership(
              email: m['email'] as String? ?? '',
              role: m['role'] as String? ?? '',
              isActive: m['isActive'] as bool? ?? false,
              provisioning: m['provisioning'] as String? ?? '',
              userId: m['userId'] as String?,
            ),
          )
          .toList(),
      createdAt: _date(json['createdAt']),
      updatedAt: _date(json['updatedAt']),
    );
  }
}

/// Everything the cadastro needs to be born.
///
/// The owner's e-mail is part of it because registering and granting the
/// owner's access is **one** operation: a tenant nobody can open is the state
/// no one notices until they need it.
class RegisterTenantInput {
  /// Creates the input.
  const RegisterTenantInput({
    required this.kind,
    required this.legalName,
    required this.tradeName,
    required this.primaryTaxId,
    required this.contactEmail,
    required this.contactPhone,
    required this.address,
    required this.ownerEmail,
    required this.products,
    this.id,
  });

  /// `Individual` or `Company`.
  final String kind;

  /// Civil or corporate name.
  final String legalName;

  /// Trade name — must be empty for an individual.
  final String tradeName;

  /// CPF or CNPJ.
  final String primaryTaxId;

  /// The tenant's contact e-mail.
  final String contactEmail;

  /// The tenant's phone, optional.
  final String contactPhone;

  /// The tenant's address.
  final TenantAddress address;

  /// Who answers for the tenant and receives the access invitation.
  final String ownerEmail;

  /// Products to enable right away.
  final List<String> products;

  /// An id to reuse instead of issuing a new one.
  ///
  /// This is what lets a cadastro that already has an identity elsewhere be
  /// migrated without reissuing anyone's access.
  final String? id;

  /// Serializes the input for `POST /api/v1/tenants`.
  Map<String, dynamic> toJson() {
    return {
      'kind': kind,
      'legalName': legalName.trim(),
      'tradeName': tradeName.trim().isEmpty ? null : tradeName.trim(),
      'primaryTaxId': primaryTaxId,
      'contactEmail': contactEmail.trim(),
      'contactPhone':
          contactPhone.trim().isEmpty ? null : contactPhone.trim(),
      'address': addressToJson(address),
      'ownerEmail': ownerEmail.trim(),
      'products': products,
      if (id != null && id!.isNotEmpty) 'id': id,
    };
  }
}

/// Serializes an address the way every write endpoint expects it.
Map<String, dynamic> addressToJson(TenantAddress address) {
  return {
    'zipCode': address.zipCode,
    'street': address.street.trim(),
    'number': address.number.trim(),
    'complement':
        address.complement.trim().isEmpty ? null : address.complement.trim(),
    'neighborhood': address.neighborhood.trim(),
    'city': address.city.trim(),
    'state': address.state.trim(),
    'country': address.country.trim().isEmpty ? null : address.country.trim(),
  };
}
