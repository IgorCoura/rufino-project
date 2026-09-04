import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

/// The in-memory answer to "may this person do this?".
///
/// Two behaviours carry the risk: a failed reload must not silently revoke
/// access the user already had, and a logout must not leave the previous
/// user's permissions visible to the next one.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late FakePermissionRepository repository;

  setUp(() => repository = FakePermissionRepository());

  PermissionNotifier buildNotifier() {
    final notifier = PermissionNotifier(permissionRepository: repository);
    addTearDown(notifier.dispose);
    return notifier;
  }

  group('PermissionNotifier before anything is loaded', () {
    test('starts out loading with no permissions and no error', () {
      final notifier = buildNotifier();

      expect(notifier.status, PermissionStatus.loading);
      expect(notifier.permissions, isEmpty);
      expect(notifier.lastError, isNull);
    });

    test('denies every question while nothing is loaded', () {
      final notifier = buildNotifier();

      expect(notifier.hasPermission('employee', 'view'), isFalse);
      expect(notifier.hasAnyScope('employee'), isFalse);
    });
  });

  group('PermissionNotifier answering permission questions', () {
    test('grants a scope that was returned for that resource', () async {
      repository.remotePermissions = [
        grant('employee', ['view', 'create']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'view'), isTrue);
      expect(notifier.hasPermission('employee', 'create'), isTrue);
    });

    test('denies a scope that was not granted on a resource the user has',
        () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'delete'), isFalse);
    });

    test('denies a scope granted on a different resource', () async {
      repository.remotePermissions = [
        grant('department', ['delete']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'delete'), isFalse);
    });

    test('reports any access at all on a resource for module visibility',
        () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.hasAnyScope('employee'), isTrue);
      expect(notifier.hasAnyScope('department'), isFalse);
    });

    test('reports access on a resource returned with an empty scope list',
        () async {
      repository.remotePermissions = [grant('employee', const [])];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.hasAnyScope('employee'), isTrue);
      expect(notifier.hasPermission('employee', 'view'), isFalse);
    });

    test('exposes the permission set as a read-only view', () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(
        () => notifier.permissions.add(grant('bill', ['pay'])),
        throwsUnsupportedError,
      );
    });
  });

  group('PermissionNotifier loading', () {
    test('ends in the loaded state once the fetch succeeds', () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.loaded);
      expect(notifier.lastError, isNull);
    });

    test('is loaded, not error, when the user legitimately has no permissions',
        () async {
      repository.remotePermissions = const [];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.loaded);
      expect(notifier.permissions, isEmpty);
    });

    test('persists what it fetched so the next launch starts warm', () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(repository.cached, isNotNull);
      expect(repository.cached!.single.resource, 'employee');
    });

    test('shows cached permissions before the network answers', () async {
      repository.cached = [
        grant('employee', ['view']),
      ];
      repository.remotePermissions = [
        grant('employee', ['view', 'create']),
      ];
      final notifier = buildNotifier();
      final statusesSeen = <PermissionStatus>[];
      notifier.addListener(() => statusesSeen.add(notifier.status));

      await notifier.loadPermissions();

      expect(statusesSeen, contains(PermissionStatus.loaded));
      expect(notifier.hasPermission('employee', 'create'), isTrue);
    });

    test('ignores an empty cached set and waits for the network', () async {
      repository.cached = const [];
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'view'), isTrue);
    });

    test('replaces the permissions it already had with the fresh ones',
        () async {
      repository.remotePermissions = [
        grant('employee', ['view', 'create']),
      ];
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      await notifier.loadPermissions();

      expect(notifier.hasPermission('employee', 'create'), isFalse);
      expect(notifier.hasPermission('employee', 'view'), isTrue);
    });

    test('notifies its listeners so guards rebuild', () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();
      var notifications = 0;
      notifier.addListener(() => notifications++);

      await notifier.loadPermissions();

      expect(notifications, greaterThan(0));
    });
  });

  group('PermissionNotifier when the fetch fails', () {
    test('reports an error state when there is nothing to fall back on',
        () async {
      repository.remoteError = Exception('keycloak unreachable');
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.error);
      expect(notifier.permissions, isEmpty);
      expect(notifier.lastError, isNotNull);
    });

    test('keeps the permissions it already had instead of revoking access',
        () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      repository.remoteError = Exception('keycloak unreachable');
      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.loaded);
      expect(notifier.hasPermission('employee', 'view'), isTrue);
    });

    test('still records the failure while serving the stale permissions',
        () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      repository.remoteError = StateError('keycloak unreachable');
      await notifier.loadPermissions();

      expect(notifier.lastError, isA<StateError>());
    });

    test('forgets the previous error once a fetch succeeds again', () async {
      repository.remoteError = Exception('keycloak unreachable');
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      repository.remoteError = null;
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      await notifier.loadPermissions();

      expect(notifier.lastError, isNull);
      expect(notifier.status, PermissionStatus.loaded);
    });

    test('falls back to the cache when the network is down', () async {
      repository.cached = [
        grant('employee', ['view']),
      ];
      repository.remoteError = Exception('keycloak unreachable');
      final notifier = buildNotifier();

      await notifier.loadPermissions();

      expect(notifier.status, PermissionStatus.loaded);
      expect(notifier.hasPermission('employee', 'view'), isTrue);
    });
  });

  group('PermissionNotifier on logout', () {
    test('drops every permission the previous user had', () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      await notifier.clear();

      expect(notifier.permissions, isEmpty);
      expect(notifier.hasPermission('employee', 'view'), isFalse);
      expect(notifier.hasAnyScope('employee'), isFalse);
    });

    test('returns to the loading state so guards do not render an error',
        () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      await notifier.clear();

      expect(notifier.status, PermissionStatus.loading);
      expect(notifier.lastError, isNull);
    });

    test('wipes the persisted copy as well as the in-memory one', () async {
      repository.remotePermissions = [
        grant('employee', ['view']),
      ];
      final notifier = buildNotifier();
      await notifier.loadPermissions();

      await notifier.clear();

      expect(repository.clearCount, 1);
      expect(repository.cached, isNull);
    });

    test('notifies its listeners so guards hide what they were showing',
        () async {
      final notifier = buildNotifier();
      var notifications = 0;
      notifier.addListener(() => notifications++);

      await notifier.clear();

      expect(notifications, 1);
    });
  });
}
