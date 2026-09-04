import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late SharedPreferences prefs;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    prefs = await SharedPreferences.getInstance();
  });

  group('PermissionCacheService with two audiences', () {
    test('each audience keeps its own permissions', () async {
      final peopleManagement = PermissionCacheService(prefs: prefs);
      final tenantManagement = PermissionCacheService(
        prefs: prefs,
        cacheKey: 'cached_permissions_tenant_management',
      );

      await peopleManagement.save(
        const [Permission(resource: 'employee', scopes: ['view'])],
      );
      await tenantManagement.save(
        const [Permission(resource: 'tenant', scopes: ['create'])],
      );

      // Sem chave própria uma sobrescreveria a outra, e qual venceria
      // dependeria da ordem em que as duas requisições terminassem.
      expect(peopleManagement.loadCached()!.single.resource, 'employee');
      expect(tenantManagement.loadCached()!.single.resource, 'tenant');
    });

    test('clearing one audience leaves the other alone', () async {
      final peopleManagement = PermissionCacheService(prefs: prefs);
      final tenantManagement = PermissionCacheService(
        prefs: prefs,
        cacheKey: 'cached_permissions_tenant_management',
      );

      await peopleManagement.save(
        const [Permission(resource: 'employee', scopes: ['view'])],
      );
      await tenantManagement.save(
        const [Permission(resource: 'tenant', scopes: ['create'])],
      );

      await tenantManagement.clear();

      expect(tenantManagement.loadCached(), isNull);
      expect(peopleManagement.loadCached(), isNotNull);
    });

    test('the default key is the one the app already had', () async {
      await PermissionCacheService(prefs: prefs).save(
        const [Permission(resource: 'employee', scopes: ['view'])],
      );

      expect(prefs.getString('cached_permissions'), isNotNull);
    });
  });
}
