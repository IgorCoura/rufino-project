import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/services/permission_cache_service.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  late PermissionCacheService cacheService;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();
    cacheService = PermissionCacheService(prefs: prefs);
  });

  group('PermissionCacheService', () {
    test('loadCached returns null when no permissions are stored', () {
      final result = cacheService.loadCached();

      expect(result, isNull);
    });

    test('save persists permissions that can be loaded back', () async {
      const permissions = [
        Permission(resource: 'employee', scopes: ['create', 'view']),
        Permission(resource: 'document', scopes: ['upload']),
      ];

      await cacheService.save(permissions);
      final loaded = cacheService.loadCached();

      expect(loaded, isNotNull);
      expect(loaded!.length, 2);
      expect(loaded[0].resource, 'employee');
      expect(loaded[0].scopes, ['create', 'view']);
      expect(loaded[1].resource, 'document');
      expect(loaded[1].scopes, ['upload']);
    });

    test('clear removes stored permissions', () async {
      const permissions = [
        Permission(resource: 'employee', scopes: ['view']),
      ];

      await cacheService.save(permissions);
      await cacheService.clear();
      final loaded = cacheService.loadCached();

      expect(loaded, isNull);
    });

    test('save overwrites previous permissions', () async {
      await cacheService.save([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);

      await cacheService.save([
        const Permission(resource: 'company', scopes: ['create', 'edit']),
      ]);

      final loaded = cacheService.loadCached();

      expect(loaded, isNotNull);
      expect(loaded!.length, 1);
      expect(loaded.first.resource, 'company');
    });

    test('loadCached handles empty list', () async {
      await cacheService.save([]);

      final loaded = cacheService.loadCached();

      expect(loaded, isNotNull);
      expect(loaded, isEmpty);
    });
  });
}
