import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../../domain/entities/permission.dart';
import '../models/permission_model.dart';

/// Persists and retrieves [Permission] data using [SharedPreferences].
///
/// On web this uses `localStorage`; on mobile/desktop it uses the platform's
/// key-value store. Permissions are not secrets — they are public
/// resource/scope pairs enforced server-side — so `SharedPreferences` is
/// sufficient (no need for `flutter_secure_storage`).
class PermissionCacheService {
  const PermissionCacheService({required SharedPreferences prefs})
      : _prefs = prefs;

  final SharedPreferences _prefs;

  static const _cacheKey = 'cached_permissions';

  /// Returns the previously cached permissions, or `null` if none are stored.
  List<Permission>? loadCached() {
    final raw = _prefs.getString(_cacheKey);
    if (raw == null) return null;

    try {
      final list = (jsonDecode(raw) as List<dynamic>)
          .map((e) => PermissionModel.fromJson(e as Map<String, dynamic>))
          .map((m) => m.toEntity())
          .toList();
      return list;
    } catch (_) {
      return null;
    }
  }

  /// Persists the given [permissions] for later retrieval.
  Future<void> save(List<Permission> permissions) async {
    final json = permissions
        .map((p) => PermissionModel.fromEntity(p).toJson())
        .toList();
    await _prefs.setString(_cacheKey, jsonEncode(json));
  }

  /// Removes the cached permissions.
  Future<void> clear() async {
    await _prefs.remove(_cacheKey);
  }
}
