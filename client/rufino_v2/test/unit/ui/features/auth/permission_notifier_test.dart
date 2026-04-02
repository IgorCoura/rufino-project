import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';

import '../../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakePermissionRepository repository;
  late PermissionNotifier notifier;

  setUp(() {
    WidgetsFlutterBinding.ensureInitialized();
    repository = FakePermissionRepository();
    notifier = PermissionNotifier(permissionRepository: repository);
  });

  tearDown(() => notifier.dispose());

  group('PermissionNotifier', () {
    test('initial status is loading', () {
      expect(notifier.status, PermissionStatus.loading);
    });

    test('loadPermissions sets status to loaded on success', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['create', 'view']),
      ]);

      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.loaded);
    });

    test('loadPermissions sets status to error when first load fails',
        () async {
      repository.setShouldFail(true);

      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.error);
    });

    test('hasPermission returns true when user has the resource and scope',
        () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['create', 'view']),
        const Permission(resource: 'document', scopes: ['view']),
      ]);

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'create'), isTrue);
      expect(notifier.hasPermission('employee', 'view'), isTrue);
      expect(notifier.hasPermission('document', 'view'), isTrue);
    });

    test('hasPermission returns false when user lacks the scope', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'create'), isFalse);
    });

    test('hasPermission returns false when user lacks the resource', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);

      await notifier.loadPermissions();

      expect(notifier.hasPermission('Document', 'view'), isFalse);
    });

    test('hasAnyScope returns true when user has any scope on resource',
        () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);

      await notifier.loadPermissions();

      expect(notifier.hasAnyScope('employee'), isTrue);
    });

    test('hasAnyScope returns false when user has no scope on resource',
        () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);

      await notifier.loadPermissions();

      expect(notifier.hasAnyScope('Document'), isFalse);
    });

    test('clear resets permissions and sets status to loading', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();
      expect(notifier.hasAnyScope('employee'), isTrue);

      await notifier.clear();

      expect(notifier.status, PermissionStatus.loading);
      expect(notifier.hasAnyScope('employee'), isFalse);
    });

    test('notifies listeners when permissions load', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);

      var notifyCount = 0;
      notifier.addListener(() => notifyCount++);

      await notifier.loadPermissions();

      // loading notification + loaded notification
      expect(notifyCount, 2);
    });

    test('hasPermission returns false when permissions failed to load',
        () async {
      repository.setShouldFail(true);

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'view'), isFalse);
      expect(notifier.hasAnyScope('employee'), isFalse);
    });
  });

  group('PermissionNotifier stale permission retention', () {
    test('retains previously loaded permissions when reload fails', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['create', 'view']),
      ]);
      await notifier.loadPermissions();
      expect(notifier.hasPermission('employee', 'create'), isTrue);

      // Now make the next fetch fail.
      repository.setShouldFail(true);
      await notifier.loadPermissions();

      // Permissions should be retained, status stays loaded.
      expect(notifier.status, PermissionStatus.loaded);
      expect(notifier.hasPermission('employee', 'create'), isTrue);
      expect(notifier.hasPermission('employee', 'view'), isTrue);
    });

    test('exposes lastError after a failed reload', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();
      expect(notifier.lastError, isNull);

      repository.setShouldFail(true);
      await notifier.loadPermissions();

      expect(notifier.lastError, isNotNull);
    });

    test('clears lastError on successful reload', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();

      // Fail once.
      repository.setShouldFail(true);
      await notifier.loadPermissions();
      expect(notifier.lastError, isNotNull);

      // Succeed again.
      repository.setShouldFail(false);
      await notifier.loadPermissions();
      expect(notifier.lastError, isNull);
    });

    test('clear resets lastError', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();

      repository.setShouldFail(true);
      await notifier.loadPermissions();
      expect(notifier.lastError, isNotNull);

      await notifier.clear();
      expect(notifier.lastError, isNull);
    });
  });

  group('PermissionNotifier cache hydration', () {
    test('hydrates from cache when permissions are empty', () async {
      // Put data in cache but make the remote fetch fail.
      repository.setCachedPermissions([
        const Permission(resource: 'employee', scopes: ['view', 'create']),
      ]);
      repository.setShouldFail(true);

      await notifier.loadPermissions();

      // Should have permissions from cache even though remote failed.
      expect(notifier.hasPermission('employee', 'view'), isTrue);
      expect(notifier.hasPermission('employee', 'create'), isTrue);
      expect(notifier.status, PermissionStatus.loaded);
    });

    test('persists permissions to cache after successful fetch', () async {
      repository.setPermissions([
        const Permission(resource: 'document', scopes: ['upload']),
      ]);

      await notifier.loadPermissions();

      // The fake repository stores cached permissions set via cachePermissions.
      final cached = await repository.getCachedPermissions();
      expect(cached, isNotNull);
      expect(cached!.length, 1);
      expect(cached.first.resource, 'document');
    });

    test('clear removes cached permissions', () async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();

      await notifier.clear();

      final cached = await repository.getCachedPermissions();
      expect(cached, isNull);
    });
  });
}
