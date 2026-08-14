import 'tax_id.dart';
import 'tenant_enums.dart';

/// A row of the back-office listing — enough to find a tenant without
/// loading the whole cadastro.
class TenantSummary {
  /// Creates the row.
  const TenantSummary({
    required this.id,
    required this.kind,
    required this.legalName,
    required this.tradeName,
    required this.primaryTaxId,
    required this.status,
    required this.accessProvisioning,
    required this.contactEmail,
    required this.activeProducts,
    required this.createdAt,
  });

  /// The tenant id.
  final String id;

  /// `Individual` or `Company`.
  final String kind;

  /// Civil or corporate name.
  final String legalName;

  /// Trade name, empty for a natural person.
  final String tradeName;

  /// CPF or CNPJ.
  final String primaryTaxId;

  /// `Active` or `Suspended`.
  final String status;

  /// Aggregated state of the access grants.
  final String accessProvisioning;

  /// The tenant's contact e-mail.
  final String contactEmail;

  /// The products currently on.
  final List<String> activeProducts;

  /// When the cadastro was created.
  final DateTime createdAt;

  /// The best name to show.
  String get displayName => tradeName.isNotEmpty ? tradeName : legalName;

  /// Whether this is a natural person.
  bool get isIndividual => kind == TenantKinds.individual;

  /// Whether the cadastro is frozen.
  bool get isSuspended => status == TenantStatuses.suspended;

  /// Whether an access grant failed to reach the identity provider.
  bool get hasFailedProvisioning =>
      accessProvisioning == ProvisioningStatuses.failed;

  /// Whether some grant is still on its way.
  bool get hasPendingProvisioning =>
      accessProvisioning == ProvisioningStatuses.pending;

  /// The document formatted for reading.
  String get formattedTaxId => TaxId.format(primaryTaxId);
}

/// One page of the back-office listing.
///
/// Pagination is by cursor: [nextCursor] is `null` on the last page.
class TenantPage {
  /// Creates the page.
  const TenantPage({required this.items, this.nextCursor});

  /// An empty page, for the initial state of a list.
  static const TenantPage empty = TenantPage(items: []);

  /// The rows in this page.
  final List<TenantSummary> items;

  /// The cursor of the next page, or `null` when this is the last one.
  final String? nextCursor;

  /// Whether there is another page to ask for.
  bool get hasMore => nextCursor != null;
}

/// The filters of the back-office listing.
///
/// All optional and combinable. [search] matches name, trade name or
/// document — which is how an operator looks a customer up from whatever
/// they say on the phone.
class TenantListFilter {
  /// Creates the filter set.
  const TenantListFilter({this.kind, this.status, this.product, this.search});

  /// Restrict to `Individual` or `Company`.
  final String? kind;

  /// Restrict to `Active` or `Suspended`.
  final String? status;

  /// Restrict to tenants with this product enabled.
  final String? product;

  /// Free text: name, trade name or document.
  final String? search;

  /// Whether any filter is set.
  bool get isEmpty =>
      kind == null &&
      status == null &&
      product == null &&
      (search == null || search!.isEmpty);

  /// Returns a copy with the given fields replaced.
  ///
  /// Each field has its own `clear` flag because `null` already means "do not
  /// change this one" — without them a filter could never be removed.
  TenantListFilter copyWith({
    String? kind,
    String? status,
    String? product,
    String? search,
    bool clearKind = false,
    bool clearStatus = false,
    bool clearProduct = false,
    bool clearSearch = false,
  }) {
    return TenantListFilter(
      kind: clearKind ? null : (kind ?? this.kind),
      status: clearStatus ? null : (status ?? this.status),
      product: clearProduct ? null : (product ?? this.product),
      search: clearSearch ? null : (search ?? this.search),
    );
  }
}
