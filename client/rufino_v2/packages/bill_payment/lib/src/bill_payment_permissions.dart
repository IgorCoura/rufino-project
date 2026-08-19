import 'package:rufino_core/rufino_core.dart';

/// The permissions of this module, as declared in Keycloak.
///
/// They belong to the **`bill-payment-api`** resource server, which is not
/// the one the People Management or Tenant Management screens ask about. The
/// same resource name under two audiences would be two different permissions
/// — which is exactly why the guards below are typed.
abstract final class BillPaymentResources {
  /// A bill from capture to approval. Scopes: view, import, validate,
  /// approve, deny, cancel.
  static const String bill = 'bill';

  /// A captured artifact in the quarantine queue. Scopes: view, reprocess,
  /// claim.
  static const String captureItem = 'capture-item';

  /// A monitored mailbox. Scopes: view, manage, sync.
  static const String captureSource = 'capture-source';

  /// An expected recurring bill. Scopes: view, manage, waive.
  static const String expectation = 'expectation';

  /// A registered beneficiary. Scopes: view, manage.
  static const String payee = 'payee';

  /// The tenant's payer profile (one per tenant). Scopes: view, manage.
  static const String payerProfile = 'payer-profile';

  /// A trusted or blocked sender. Scopes: view, manage.
  ///
  /// The authorization resource is named `origin` — singular, without the
  /// `trusted` prefix — even though the route is `/trusted-origins`.
  static const String origin = 'origin';
}

/// The scope names shared by the resources above.
abstract final class BillPaymentScopes {
  /// Read.
  static const String view = 'view';

  /// Import a bill manually.
  static const String import = 'import';

  /// Re-run the official lookup and the twelve checks.
  static const String validate = 'validate';

  /// Authorize the payment — the sensitive action of this module.
  static const String approve = 'approve';

  /// Refuse a bill.
  static const String deny = 'deny';

  /// Remove a bill from the flow.
  static const String cancel = 'cancel';

  /// Send a quarantined item back through the extraction cascade. Has its
  /// own scope because it spends the vision extractor's daily quota.
  static const String reprocess = 'reprocess';

  /// Claim an unrouted item as this tenant's bill.
  static const String claim = 'claim';

  /// Create, edit or remove a cadastro.
  static const String manage = 'manage';

  /// Trigger a mailbox sync. Routine work, separate from changing the
  /// credential.
  static const String sync = 'sync';

  /// Dismiss an expectation cycle — silences the safety net, so it has its
  /// own scope.
  static const String waive = 'waive';
}

/// Holds the permissions granted on the `bill-payment-api` audience.
///
/// A subclass exists for one reason: `provider` resolves by type, and the app
/// keeps three notifiers of the same shape in the tree. Without the distinct
/// type, whichever was registered last would answer for every audience.
class BillPaymentPermissionNotifier extends PermissionNotifier {
  /// Creates the notifier over the bill payment audience's repository.
  BillPaymentPermissionNotifier({required super.permissionRepository});

  /// Whether the person can decide the destiny of a bill.
  bool get canDecide =>
      hasPermission(BillPaymentResources.bill, BillPaymentScopes.approve);
}

/// Renders `child` only when the bill payment audience grants `scope` on
/// `resource`.
typedef BillPaymentPermissionGuard
    = PermissionGuard<BillPaymentPermissionNotifier>;

/// Renders `child` only when the bill payment audience grants any scope on
/// `resource`.
typedef BillPaymentModuleGuard = ModuleGuard<BillPaymentPermissionNotifier>;
