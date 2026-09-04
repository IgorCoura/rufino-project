import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

/// The one place the app remembers who the user is operating as.
///
/// A relaunch that lands on the wrong tenant, or on none at all, is the worst
/// failure this class can have — so restore is exercised against an empty
/// store, a good payload and a corrupt one.
void main() {
  late FakeSecureStorage storage;
  late TenantContextNotifier context;

  setUp(() {
    storage = FakeSecureStorage();
    context = TenantContextNotifier(storage: storage);
    addTearDown(context.dispose);
  });

  group('TenantContextNotifier before a tenant is chosen', () {
    test('has no tenant and no tenant id', () {
      expect(context.current, isNull);
      expect(context.hasTenant, isFalse);
      expect(context.tenantId, isNull);
    });

    test('denies every product, because no context means no product', () {
      expect(context.hasProduct(TenantProducts.billPayment), isFalse);
      expect(context.hasProduct(TenantProducts.peopleManagement), isFalse);
    });
  });

  group('TenantContextNotifier selecting a tenant', () {
    test('becomes the current context', () async {
      await context.select(tenant(id: 'tenant-7'));

      expect(context.hasTenant, isTrue);
      expect(context.tenantId, 'tenant-7');
      expect(context.current!.id, 'tenant-7');
    });

    test('answers about the products of the tenant just chosen', () async {
      await context.select(
        tenant(activeProducts: const [TenantProducts.billPayment]),
      );

      expect(context.hasProduct(TenantProducts.billPayment), isTrue);
      expect(context.hasProduct(TenantProducts.peopleManagement), isFalse);
    });

    test('persists the choice so a relaunch does not ask again', () async {
      await context.select(tenant(id: 'tenant-7'));

      expect(storage.values['selected_tenant'], isNotNull);
      expect(
        jsonDecode(storage.values['selected_tenant']!),
        containsPair('id', 'tenant-7'),
      );
    });

    test('notifies listeners so the shell can rebuild', () async {
      var notifications = 0;
      context.addListener(() => notifications++);

      await context.select(tenant());

      expect(notifications, 1);
    });

    test('replaces the previous tenant instead of stacking a second one',
        () async {
      await context.select(tenant(id: 'tenant-1'));

      await context.select(tenant(id: 'tenant-2'));

      expect(context.tenantId, 'tenant-2');
    });
  });

  group('TenantContextNotifier restoring on launch', () {
    test('answers null and stays empty when nothing was ever stored',
        () async {
      final restored = await context.restore();

      expect(restored, isNull);
      expect(context.hasTenant, isFalse);
    });

    test('brings back exactly the tenant that was selected before', () async {
      await context.select(
        tenant(
          id: 'tenant-7',
          tradeName: 'Rufino',
          activeProducts: const [TenantProducts.billPayment],
        ),
      );
      final fresh = TenantContextNotifier(storage: storage);
      addTearDown(fresh.dispose);

      final restored = await fresh.restore();

      expect(restored, isNotNull);
      expect(fresh.tenantId, 'tenant-7');
      expect(fresh.current!.displayName, 'Rufino');
      expect(fresh.hasProduct(TenantProducts.billPayment), isTrue);
    });

    test('leaves the app without a tenant when the stored payload cannot be '
        'read', () async {
      storage.values['selected_tenant'] = 'not json at all';

      final restored = await context.restore();

      expect(restored, isNull);
      expect(context.hasTenant, isFalse);
    });

    test('evicts the unreadable payload so the next launch does not retry it',
        () async {
      storage.values['selected_tenant'] = '{"kind":"Company"}';

      await context.restore();

      expect(storage.values.containsKey('selected_tenant'), isFalse);
      expect(storage.deletedKeys, contains('selected_tenant'));
    });

    test('drops the tenant it was holding when the stored payload is corrupt',
        () async {
      await context.select(tenant(id: 'tenant-7'));
      storage.values['selected_tenant'] = 'corrupt';

      await context.restore();

      expect(context.hasTenant, isFalse);
    });

    test('notifies listeners once a stored tenant has been read', () async {
      await context.select(tenant(id: 'tenant-7'));
      final fresh = TenantContextNotifier(storage: storage);
      addTearDown(fresh.dispose);
      var notifications = 0;
      fresh.addListener(() => notifications++);

      await fresh.restore();

      expect(notifications, 1);
    });

    test('stays quiet when there was nothing to restore', () async {
      var notifications = 0;
      context.addListener(() => notifications++);

      await context.restore();

      expect(notifications, 0);
    });
  });

  group('TenantContextNotifier clearing the context', () {
    test('forgets the tenant in memory and on disk', () async {
      await context.select(tenant(id: 'tenant-7'));

      await context.clear();

      expect(context.hasTenant, isFalse);
      expect(context.tenantId, isNull);
      expect(storage.values.containsKey('selected_tenant'), isFalse);
    });

    test('leaves nothing for a later restore to bring back', () async {
      await context.select(tenant(id: 'tenant-7'));
      await context.clear();

      expect(await context.restore(), isNull);
    });

    test('notifies listeners so the app returns to tenant selection',
        () async {
      await context.select(tenant());
      var notifications = 0;
      context.addListener(() => notifications++);

      await context.clear();

      expect(notifications, 1);
    });
  });
}
