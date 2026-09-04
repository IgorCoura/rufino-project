import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../storage/secure_storage.dart';
import 'selected_tenant.dart';

/// Holds the tenant the user is operating as, for the whole app.
///
/// There is exactly one of these. Both products read the current tenant from
/// here, which is what lets the user choose a context **once** instead of
/// once per product — and what keeps `bill_payment` from having to depend on
/// the package that owns the selection screen.
///
/// The choice is persisted so a relaunch lands straight on the home screen;
/// [restore] rehydrates it and [select] replaces it.
class TenantContextNotifier extends ChangeNotifier {
  /// Creates the notifier persisting through [storage].
  TenantContextNotifier({required SecureStorage storage}) : _storage = storage;

  final SecureStorage _storage;

  static const _storageKey = 'selected_tenant';

  SelectedTenant? _current;

  /// The tenant currently selected, or `null` when none is.
  SelectedTenant? get current => _current;

  /// Whether a tenant is selected.
  bool get hasTenant => _current != null;

  /// The current tenant id, or `null` when no tenant is selected.
  ///
  /// This is the value the products put in their routes.
  String? get tenantId => _current?.id;

  /// Whether the current tenant has [product] enabled.
  ///
  /// Returns `false` when no tenant is selected — no context, no product.
  bool hasProduct(String product) => _current?.hasProduct(product) ?? false;

  /// Reloads the previously selected tenant from storage.
  ///
  /// Returns the restored tenant, or `null` when nothing was stored or the
  /// stored value could not be read.
  Future<SelectedTenant?> restore() async {
    final raw = await _storage.read(key: _storageKey);
    if (raw == null) return null;

    try {
      _current =
          SelectedTenant.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      // A payload we cannot read is the same as no selection at all — the
      // user picks again instead of the app dying on launch.
      _current = null;
      await _storage.delete(key: _storageKey);
    }

    notifyListeners();
    return _current;
  }

  /// Makes [tenant] the current context and persists the choice.
  Future<void> select(SelectedTenant tenant) async {
    _current = tenant;
    await _storage.write(key: _storageKey, value: jsonEncode(tenant.toJson()));
    notifyListeners();
  }

  /// Drops the current context, in memory and on disk.
  Future<void> clear() async {
    _current = null;
    await _storage.delete(key: _storageKey);
    notifyListeners();
  }
}
