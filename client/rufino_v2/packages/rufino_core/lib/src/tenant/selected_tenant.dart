/// The products a tenant can have enabled on the platform.
///
/// The strings are the wire values of the backend's `ProductCode` smart enum
/// — they travel in `GET /api/v1/me/tenants` and in the product routes of the
/// tenant back-office. They are the contract; the labels are presentation.
abstract final class TenantProducts {
  /// Employee and document management.
  static const String peopleManagement = 'PeopleManagement';

  /// Bill capture, verification, approval and payment.
  static const String billPayment = 'BillPayment';
}

/// The tenant the user is currently operating as.
///
/// This is **the** context of the app: one selection serves every product,
/// and the products read it from here instead of each keeping its own
/// selector. Built from `GET /api/v1/me/tenants`, which answers from the
/// e-mail in the caller's own token.
class SelectedTenant {
  /// Creates the selected tenant.
  const SelectedTenant({
    required this.id,
    required this.kind,
    required this.legalName,
    required this.tradeName,
    required this.status,
    required this.role,
    required List<String> activeProducts,
  }) : _activeProducts = activeProducts;

  /// Rebuilds the value from its persisted JSON form.
  factory SelectedTenant.fromJson(Map<String, dynamic> json) {
    return SelectedTenant(
      id: json['id'] as String,
      kind: json['kind'] as String? ?? '',
      legalName: json['legalName'] as String? ?? '',
      tradeName: json['tradeName'] as String? ?? '',
      status: json['status'] as String? ?? '',
      role: json['role'] as String? ?? '',
      activeProducts: (json['activeProducts'] as List<dynamic>? ?? const [])
          .map((e) => e as String)
          .toList(),
    );
  }

  /// The tenant id — the same value the products carry in the route and in
  /// the token claim.
  final String id;

  /// `Individual` or `Company`.
  final String kind;

  /// Civil name (individual) or corporate name (company).
  final String legalName;

  /// Trade name. Always empty for an individual.
  final String tradeName;

  /// `Active` or `Suspended`.
  final String status;

  /// The caller's role inside this tenant: `Owner` or `Member`.
  final String role;

  final List<String> _activeProducts;

  /// The products enabled for this tenant.
  List<String> get activeProducts => List.unmodifiable(_activeProducts);

  /// Whether this tenant is a natural person.
  bool get isIndividual => kind == 'Individual';

  /// Whether the tenant is suspended — its cadastro exists but is frozen.
  bool get isSuspended => status == 'Suspended';

  /// Whether the caller answers for this tenant.
  bool get isOwner => role == 'Owner';

  /// The best name to show: trade name when there is one, legal name
  /// otherwise.
  String get displayName => tradeName.isNotEmpty ? tradeName : legalName;

  /// Whether [product] is enabled for this tenant.
  ///
  /// Use [TenantProducts] for the argument — a typo in a literal silently
  /// hides a whole product from the home screen.
  bool hasProduct(String product) => _activeProducts.contains(product);

  /// Serializes the value for local persistence.
  Map<String, dynamic> toJson() => {
        'id': id,
        'kind': kind,
        'legalName': legalName,
        'tradeName': tradeName,
        'status': status,
        'role': role,
        'activeProducts': _activeProducts,
      };
}
