import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../fakes/fakes.dart';

/// The local cache of Keycloak permissions.
///
/// The cache key is the whole safety mechanism: two resource servers writing
/// under one key would overwrite each other, and which one won would depend on
/// the order two unrelated requests happened to finish in. That is what the
/// audience group below pins down.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late SharedPreferences prefs;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    prefs = await SharedPreferences.getInstance();
  });

  group('PermissionCacheService', () {
    test('answers null when nothing was ever cached', () {
      final service = PermissionCacheService(prefs: prefs);

      expect(service.loadCached(), isNull);
    });

    test('returns the permissions it was given, scopes included', () async {
      final service = PermissionCacheService(prefs: prefs);

      await service.save([
        grant('employee', ['view', 'create']),
        grant('department', ['view']),
      ]);
      final loaded = service.loadCached();

      expect(loaded, hasLength(2));
      expect(loaded![0].resource, 'employee');
      expect(loaded[0].scopes, ['view', 'create']);
      expect(loaded[1].resource, 'department');
    });

    test('distinguishes an empty cache from an absent one', () async {
      final service = PermissionCacheService(prefs: prefs);

      await service.save(const []);

      expect(service.loadCached(), isEmpty);
      expect(service.loadCached(), isNotNull);
    });

    test('replaces the previous set instead of appending to it', () async {
      final service = PermissionCacheService(prefs: prefs);

      await service.save([
        grant('employee', ['view']),
      ]);
      await service.save([
        grant('bill', ['pay']),
      ]);

      expect(service.loadCached()!.map((p) => p.resource), ['bill']);
    });

    test('forgets everything after a clear', () async {
      final service = PermissionCacheService(prefs: prefs);
      await service.save([
        grant('employee', ['view']),
      ]);

      await service.clear();

      expect(service.loadCached(), isNull);
    });

    test('answers null instead of throwing when the stored payload is not '
        'readable', () async {
      await prefs.setString('cached_permissions', 'not json at all');
      final service = PermissionCacheService(prefs: prefs);

      expect(service.loadCached(), isNull);
    });

    test('answers null when the stored payload has the wrong shape', () async {
      await prefs.setString('cached_permissions', jsonEncode({'a': 1}));
      final service = PermissionCacheService(prefs: prefs);

      expect(service.loadCached(), isNull);
    });

    test('answers null when a cached entry is missing its resource name',
        () async {
      await prefs.setString(
        'cached_permissions',
        jsonEncode([
          {
            'scopes': ['view'],
          },
        ]),
      );
      final service = PermissionCacheService(prefs: prefs);

      expect(service.loadCached(), isNull);
    });

    test('stores under a documented default key when none is given', () async {
      final service = PermissionCacheService(prefs: prefs);

      await service.save([
        grant('employee', ['view']),
      ]);

      expect(prefs.getString('cached_permissions'), isNotNull);
    });
  });

  group('PermissionCacheService keyed per audience', () {
    late PermissionCacheService peopleCache;
    late PermissionCacheService tenantCache;

    setUp(() {
      peopleCache = PermissionCacheService(
        prefs: prefs,
        cacheKey: 'cached_permissions_people',
      );
      tenantCache = PermissionCacheService(
        prefs: prefs,
        cacheKey: 'cached_permissions_tenant',
      );
    });

    test('does not let one audience overwrite the other', () async {
      await peopleCache.save([
        grant('employee', ['view', 'create']),
      ]);
      await tenantCache.save([
        grant('tenant', ['view']),
      ]);

      expect(peopleCache.loadCached()!.single.resource, 'employee');
      expect(tenantCache.loadCached()!.single.resource, 'tenant');
    });

    test('keeps the same resource name apart when it exists on both', () async {
      await peopleCache.save([
        grant('report', ['view']),
      ]);
      await tenantCache.save([
        grant('report', ['view', 'export']),
      ]);

      expect(peopleCache.loadCached()!.single.scopes, ['view']);
      expect(tenantCache.loadCached()!.single.scopes, ['view', 'export']);
    });

    test('clears only the audience it was asked about', () async {
      await peopleCache.save([
        grant('employee', ['view']),
      ]);
      await tenantCache.save([
        grant('tenant', ['view']),
      ]);

      await peopleCache.clear();

      expect(peopleCache.loadCached(), isNull);
      expect(tenantCache.loadCached(), isNotNull);
    });

    test('leaves an audience blind to what the other cached', () async {
      await peopleCache.save([
        grant('employee', ['view']),
      ]);

      expect(tenantCache.loadCached(), isNull);
    });
  });
}
