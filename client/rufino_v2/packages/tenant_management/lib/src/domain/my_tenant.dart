import 'package:rufino_core/rufino_core.dart';

import 'tenant_enums.dart';

/// A tenant the signed-in person has access to.
///
/// Comes from `GET /api/v1/me/tenants`, which answers from the e-mail in the
/// caller's own token — never from a parameter, so the endpoint cannot be
/// used to discover who belongs where.
class MyTenant {
  /// Creates the entry.
  const MyTenant({
    required this.id,
    required this.kind,
    required this.legalName,
    required this.tradeName,
    required this.status,
    required this.role,
    required this.activeProducts,
  });

  /// The tenant id.
  final String id;

  /// `Individual` or `Company`.
  final String kind;

  /// Civil or corporate name.
  final String legalName;

  /// Trade name, empty for a natural person.
  final String tradeName;

  /// `Active` or `Suspended`.
  final String status;

  /// The caller's role inside this tenant.
  final String role;

  /// The products enabled for this tenant.
  final List<String> activeProducts;

  /// The best name to show.
  String get displayName => tradeName.isNotEmpty ? tradeName : legalName;

  /// Whether this is a natural person.
  bool get isIndividual => kind == TenantKinds.individual;

  /// Whether the cadastro is frozen.
  ///
  /// A suspended tenant is still shown — hiding it would be lying about a
  /// cadastro that exists — but it cannot be entered.
  bool get isSuspended => status == TenantStatuses.suspended;

  /// Whether it can be selected as the current context.
  bool get isSelectable => !isSuspended;

  /// The caller's role label.
  String get roleLabel => MembershipRoles.label(role);

  /// Turns this entry into the app-wide selection carried by `rufino_core`.
  SelectedTenant toSelectedTenant() {
    return SelectedTenant(
      id: id,
      kind: kind,
      legalName: legalName,
      tradeName: tradeName,
      status: status,
      role: role,
      activeProducts: activeProducts,
    );
  }
}
