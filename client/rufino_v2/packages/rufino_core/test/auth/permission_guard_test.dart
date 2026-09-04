import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

/// The notifier a second resource server would get, exactly as the docs
/// prescribe: a subclass, so `provider` can tell the audiences apart by type.
class _TenantPermissionNotifier extends PermissionNotifier {
  _TenantPermissionNotifier({required super.permissionRepository});
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  /// A notifier already loaded with [permissions].
  Future<PermissionNotifier> loadedWith(List<Permission> permissions) async {
    final repository = FakePermissionRepository()
      ..remotePermissions = permissions;
    final notifier = PermissionNotifier(permissionRepository: repository);
    addTearDown(notifier.dispose);
    await notifier.loadPermissions();
    return notifier;
  }

  Future<_TenantPermissionNotifier> tenantNotifierLoadedWith(
    List<Permission> permissions,
  ) async {
    final repository = FakePermissionRepository()
      ..remotePermissions = permissions;
    final notifier =
        _TenantPermissionNotifier(permissionRepository: repository);
    addTearDown(notifier.dispose);
    await notifier.loadPermissions();
    return notifier;
  }

  Widget wrap(PermissionNotifier notifier, Widget child) {
    return ChangeNotifierProvider<PermissionNotifier>.value(
      value: notifier,
      child: Directionality(textDirection: TextDirection.ltr, child: child),
    );
  }

  group('PermissionGuard', () {
    testWidgets('shows its child when the user holds the scope', (tester) async {
      final notifier = await loadedWith([
        grant('employee', ['create']),
      ]);

      await tester.pumpWidget(
        wrap(
          notifier,
          const PermissionGuard(
            resource: 'employee',
            scope: 'create',
            child: Text('new employee'),
          ),
        ),
      );

      expect(find.text('new employee'), findsOneWidget);
    });

    testWidgets('hides its child when the scope is missing on a resource the '
        'user otherwise has', (tester) async {
      final notifier = await loadedWith([
        grant('employee', ['view']),
      ]);

      await tester.pumpWidget(
        wrap(
          notifier,
          const PermissionGuard(
            resource: 'employee',
            scope: 'create',
            child: Text('new employee'),
          ),
        ),
      );

      expect(find.text('new employee'), findsNothing);
    });

    testWidgets('collapses to nothing rather than disabling the child',
        (tester) async {
      final notifier = await loadedWith(const []);

      await tester.pumpWidget(
        wrap(
          notifier,
          const PermissionGuard(
            resource: 'employee',
            scope: 'create',
            child: Text('new employee'),
          ),
        ),
      );

      final box = tester.widget<SizedBox>(find.byType(SizedBox));
      expect(box.width, 0);
      expect(box.height, 0);
    });

    testWidgets('reveals its child when permissions arrive later',
        (tester) async {
      final repository = FakePermissionRepository();
      final notifier = PermissionNotifier(permissionRepository: repository);
      addTearDown(notifier.dispose);

      await tester.pumpWidget(
        wrap(
          notifier,
          const PermissionGuard(
            resource: 'employee',
            scope: 'create',
            child: Text('new employee'),
          ),
        ),
      );
      expect(find.text('new employee'), findsNothing);

      repository.remotePermissions = [
        grant('employee', ['create']),
      ];
      await notifier.loadPermissions();
      await tester.pump();

      expect(find.text('new employee'), findsOneWidget);
    });
  });

  group('ModuleGuard', () {
    testWidgets('shows its child when the user holds any scope at all',
        (tester) async {
      final notifier = await loadedWith([
        grant('employee', ['view']),
      ]);

      await tester.pumpWidget(
        wrap(
          notifier,
          const ModuleGuard(
            resource: 'employee',
            child: Text('employees card'),
          ),
        ),
      );

      expect(find.text('employees card'), findsOneWidget);
    });

    testWidgets('hides its child when the user has no access to the resource',
        (tester) async {
      final notifier = await loadedWith([
        grant('department', ['view']),
      ]);

      await tester.pumpWidget(
        wrap(
          notifier,
          const ModuleGuard(
            resource: 'employee',
            child: Text('employees card'),
          ),
        ),
      );

      expect(find.text('employees card'), findsNothing);
    });
  });

  group('PermissionGuard across two audiences', () {
    testWidgets('a resource granted on one audience does not unlock a guard '
        'written against the other', (tester) async {
      final peopleNotifier = await loadedWith([
        grant('report', ['view']),
      ]);
      final tenantNotifier = await tenantNotifierLoadedWith(const []);

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<PermissionNotifier>.value(
              value: peopleNotifier,
            ),
            ChangeNotifierProvider<_TenantPermissionNotifier>.value(
              value: tenantNotifier,
            ),
          ],
          child: const Directionality(
            textDirection: TextDirection.ltr,
            child: PermissionGuard<_TenantPermissionNotifier>(
              resource: 'report',
              scope: 'view',
              child: Text('tenant report'),
            ),
          ),
        ),
      );

      expect(find.text('tenant report'), findsNothing);
    });

    testWidgets('the guard reads the audience named by its type argument',
        (tester) async {
      final peopleNotifier = await loadedWith(const []);
      final tenantNotifier = await tenantNotifierLoadedWith([
        grant('report', ['view']),
      ]);

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<PermissionNotifier>.value(
              value: peopleNotifier,
            ),
            ChangeNotifierProvider<_TenantPermissionNotifier>.value(
              value: tenantNotifier,
            ),
          ],
          child: const Directionality(
            textDirection: TextDirection.ltr,
            child: PermissionGuard<_TenantPermissionNotifier>(
              resource: 'report',
              scope: 'view',
              child: Text('tenant report'),
            ),
          ),
        ),
      );

      expect(find.text('tenant report'), findsOneWidget);
    });
  });
}
