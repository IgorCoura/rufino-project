import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../testing/fakes/fake_permission_repository.dart';
import '../../../testing/fakes/fake_secure_storage.dart';

/// Stand-in for the tenant module's notifier: a second audience, so the test
/// can prove one audience never answers for the other.
class _OtherAudienceNotifier extends PermissionNotifier {
  _OtherAudienceNotifier({required super.permissionRepository});
}

const _tenantWithPeopleManagement = SelectedTenant(
  id: '11111111-1111-1111-1111-111111111111',
  kind: 'Company',
  legalName: 'Padaria do Zé LTDA',
  tradeName: '',
  status: 'Active',
  role: 'Owner',
  activeProducts: [TenantProducts.peopleManagement],
);

Future<PermissionNotifier> _notifierWith(List<Permission> permissions) async {
  final repo = FakePermissionRepository()..setPermissions(permissions);
  final notifier = PermissionNotifier(permissionRepository: repo);
  await notifier.loadPermissions();
  return notifier;
}

Future<_OtherAudienceNotifier> _otherNotifierWith(
  List<Permission> permissions,
) async {
  final repo = FakePermissionRepository()..setPermissions(permissions);
  final notifier = _OtherAudienceNotifier(permissionRepository: repo);
  await notifier.loadPermissions();
  return notifier;
}

Widget _wrap({
  required TenantContextNotifier tenantContext,
  required PermissionNotifier permissions,
  _OtherAudienceNotifier? otherAudience,
  required Widget child,
}) {
  return MultiProvider(
    providers: [
      ChangeNotifierProvider<TenantContextNotifier>.value(value: tenantContext),
      ChangeNotifierProvider<PermissionNotifier>.value(value: permissions),
      if (otherAudience != null)
        ChangeNotifierProvider<_OtherAudienceNotifier>.value(
          value: otherAudience,
        ),
    ],
    child: MaterialApp(home: Scaffold(body: child)),
  );
}

void main() {
  group('ProductGuard', () {
    late TenantContextNotifier tenantContext;
    late PermissionNotifier permissions;

    setUp(() async {
      tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
      permissions = await _notifierWith(const []);
    });

    testWidgets('renders nothing while no tenant is selected', (tester) async {
      await tester.pumpWidget(
        _wrap(
          tenantContext: tenantContext,
          permissions: permissions,
          child: const ProductGuard(
            product: TenantProducts.peopleManagement,
            child: Text('Funcionários'),
          ),
        ),
      );

      expect(find.text('Funcionários'), findsNothing);
    });

    testWidgets('shows the feature when the tenant has the product on',
        (tester) async {
      await tenantContext.select(_tenantWithPeopleManagement);

      await tester.pumpWidget(
        _wrap(
          tenantContext: tenantContext,
          permissions: permissions,
          child: const ProductGuard(
            product: TenantProducts.peopleManagement,
            child: Text('Funcionários'),
          ),
        ),
      );

      expect(find.text('Funcionários'), findsOneWidget);
    });

    testWidgets('hides a product the tenant never enabled', (tester) async {
      await tenantContext.select(_tenantWithPeopleManagement);

      await tester.pumpWidget(
        _wrap(
          tenantContext: tenantContext,
          permissions: permissions,
          child: const ProductGuard(
            product: TenantProducts.billPayment,
            child: Text('Boletos'),
          ),
        ),
      );

      expect(find.text('Boletos'), findsNothing);
    });
  });

  group('PermissionGuard across audiences', () {
    testWidgets('a permission granted on one audience does not unlock a guard '
        'written against the other', (tester) async {
      final tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
      // Same resource name, different resource servers.
      final peopleManagement = await _notifierWith(
        const [Permission(resource: 'tenant', scopes: ['create'])],
      );
      final tenantManagement = await _otherNotifierWith(const []);

      await tester.pumpWidget(
        _wrap(
          tenantContext: tenantContext,
          permissions: peopleManagement,
          otherAudience: tenantManagement,
          child: const Column(
            children: [
              PermissionGuard(
                resource: 'tenant',
                scope: 'create',
                child: Text('audiência padrão'),
              ),
              PermissionGuard<_OtherAudienceNotifier>(
                resource: 'tenant',
                scope: 'create',
                child: Text('outra audiência'),
              ),
            ],
          ),
        ),
      );

      expect(find.text('audiência padrão'), findsOneWidget);
      expect(find.text('outra audiência'), findsNothing);
    });

    testWidgets('ModuleGuard reads the audience named by its type parameter',
        (tester) async {
      final tenantContext = TenantContextNotifier(storage: FakeSecureStorage());
      final peopleManagement = await _notifierWith(const []);
      final tenantManagement = await _otherNotifierWith(
        const [Permission(resource: 'tenant', scopes: ['view'])],
      );

      await tester.pumpWidget(
        _wrap(
          tenantContext: tenantContext,
          permissions: peopleManagement,
          otherAudience: tenantManagement,
          child: const Column(
            children: [
              ModuleGuard(resource: 'tenant', child: Text('padrão')),
              ModuleGuard<_OtherAudienceNotifier>(
                resource: 'tenant',
                child: Text('back-office'),
              ),
            ],
          ),
        ),
      );

      expect(find.text('padrão'), findsNothing);
      expect(find.text('back-office'), findsOneWidget);
    });
  });
}
