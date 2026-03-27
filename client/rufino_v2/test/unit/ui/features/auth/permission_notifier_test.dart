import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';

import '../../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakePermissionRepository repository;
  late PermissionNotifier notifier;

  setUp(() {
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

    test('loadPermissions sets status to error on failure', () async {
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

      notifier.clear();

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
}
