import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_v2/domain/entities/permission.dart';
import 'package:rufino_v2/ui/core/widgets/permission_guard.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/permission_notifier.dart';

import '../../../testing/fakes/fake_permission_repository.dart';

void main() {
  late FakePermissionRepository repository;
  late PermissionNotifier notifier;

  setUp(() {
    repository = FakePermissionRepository();
    notifier = PermissionNotifier(permissionRepository: repository);
  });

  tearDown(() => notifier.dispose());

  Widget buildApp(Widget child) {
    return ChangeNotifierProvider<PermissionNotifier>.value(
      value: notifier,
      child: MaterialApp(home: Scaffold(body: child)),
    );
  }

  group('PermissionGuard', () {
    testWidgets('renders child when user has the required permission',
        (tester) async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['create', 'view']),
      ]);
      await notifier.loadPermissions();

      await tester.pumpWidget(buildApp(
        const PermissionGuard(
          resource: 'employee',
          scope: 'create',
          child: Text('Create Employee'),
        ),
      ));

      expect(find.text('Create Employee'), findsOneWidget);
    });

    testWidgets('hides child when user lacks the required scope',
        (tester) async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();

      await tester.pumpWidget(buildApp(
        const PermissionGuard(
          resource: 'employee',
          scope: 'create',
          child: Text('Create Employee'),
        ),
      ));

      expect(find.text('Create Employee'), findsNothing);
      expect(find.byType(SizedBox), findsOneWidget);
    });

    testWidgets('hides child when user lacks the required resource',
        (tester) async {
      repository.setPermissions([
        const Permission(resource: 'document', scopes: ['create']),
      ]);
      await notifier.loadPermissions();

      await tester.pumpWidget(buildApp(
        const PermissionGuard(
          resource: 'employee',
          scope: 'create',
          child: Text('Create Employee'),
        ),
      ));

      expect(find.text('Create Employee'), findsNothing);
    });

    testWidgets('hides child when no permissions are loaded', (tester) async {
      await tester.pumpWidget(buildApp(
        const PermissionGuard(
          resource: 'employee',
          scope: 'create',
          child: Text('Create Employee'),
        ),
      ));

      expect(find.text('Create Employee'), findsNothing);
    });
  });

  group('ModuleGuard', () {
    testWidgets('renders child when user has any scope on the resource',
        (tester) async {
      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();

      await tester.pumpWidget(buildApp(
        const ModuleGuard(
          resource: 'employee',
          child: Text('Employee Module'),
        ),
      ));

      expect(find.text('Employee Module'), findsOneWidget);
    });

    testWidgets('hides child when user has no scope on the resource',
        (tester) async {
      repository.setPermissions([
        const Permission(resource: 'document', scopes: ['view']),
      ]);
      await notifier.loadPermissions();

      await tester.pumpWidget(buildApp(
        const ModuleGuard(
          resource: 'employee',
          child: Text('Employee Module'),
        ),
      ));

      expect(find.text('Employee Module'), findsNothing);
    });

    testWidgets('rebuilds when permissions change', (tester) async {
      await tester.pumpWidget(buildApp(
        const ModuleGuard(
          resource: 'employee',
          child: Text('Employee Module'),
        ),
      ));

      expect(find.text('Employee Module'), findsNothing);

      repository.setPermissions([
        const Permission(resource: 'employee', scopes: ['view']),
      ]);
      await notifier.loadPermissions();
      await tester.pump();

      expect(find.text('Employee Module'), findsOneWidget);
    });
  });
}
