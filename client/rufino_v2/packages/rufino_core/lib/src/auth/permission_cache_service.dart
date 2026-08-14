import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import 'permission.dart';

/// Persists and retrieves [Permission] data using [SharedPreferences].
///
/// On web this uses `localStorage`; on mobile/desktop it uses the platform's
/// key-value store. Permissions are not secrets — they are public
/// resource/scope pairs enforced server-side — so `SharedPreferences` is
/// sufficient (no need for `flutter_secure_storage`).
///
/// Each resource server needs **its own [cacheKey]**: two audiences sharing
/// one key would overwrite each other, and which one won would depend on the
/// order the two requests happened to finish in.
class PermissionCacheService {
  /// Creates the cache backed by [prefs], storing under [cacheKey].
  const PermissionCacheService({
    required SharedPreferences prefs,
    String cacheKey = 'cached_permissions',
  })  : _prefs = prefs,
        _cacheKey = cacheKey;

  final SharedPreferences _prefs;
  final String _cacheKey;

  /// Returns the previously cached permissions, or `null` if none are stored.
  List<Permission>? loadCached() {
    final raw = _prefs.getString(_cacheKey);
    if (raw == null) return null;

    try {
      return (jsonDecode(raw) as List<dynamic>)
          .map((e) => PermissionModel.fromJson(e as Map<String, dynamic>))
          .map((m) => m.toEntity())
          .toList();
    } catch (_) {
      return null;
    }
  }

  /// Persists the given [permissions] for later retrieval.
  Future<void> save(List<Permission> permissions) async {
    final json =
        permissions.map((p) => PermissionModel.fromEntity(p).toJson()).toList();
    await _prefs.setString(_cacheKey, jsonEncode(json));
  }

  /// Removes the cached permissions.
  Future<void> clear() async {
    await _prefs.remove(_cacheKey);
  }
}
