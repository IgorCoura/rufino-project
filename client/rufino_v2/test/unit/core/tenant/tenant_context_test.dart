import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../testing/fakes/fake_secure_storage.dart';

const _tenant = SelectedTenant(
  id: '11111111-1111-1111-1111-111111111111',
  kind: 'Company',
  legalName: 'Padaria do Zé LTDA',
  tradeName: 'Pão Quente',
  status: 'Active',
  role: 'Owner',
  activeProducts: [TenantProducts.peopleManagement],
);

void main() {
  group('SelectedTenant', () {
    test('prefers the trade name for display and falls back to the legal one',
        () {
      expect(_tenant.displayName, 'Pão Quente');

      const individual = SelectedTenant(
        id: 'x',
        kind: 'Individual',
        legalName: 'José da Silva',
        tradeName: '',
        status: 'Active',
        role: 'Member',
        activeProducts: [],
      );
      expect(individual.displayName, 'José da Silva');
      expect(individual.isIndividual, isTrue);
      expect(individual.isOwner, isFalse);
    });

    test('reports the products it carries and nothing else', () {
      expect(_tenant.hasProduct(TenantProducts.peopleManagement), isTrue);
      expect(_tenant.hasProduct(TenantProducts.billPayment), isFalse);
    });

    test('round-trips through JSON without losing a field', () {
      final restored = SelectedTenant.fromJson(_tenant.toJson());

      expect(restored.id, _tenant.id);
      expect(restored.kind, _tenant.kind);
      expect(restored.legalName, _tenant.legalName);
      expect(restored.tradeName, _tenant.tradeName);
      expect(restored.status, _tenant.status);
      expect(restored.role, _tenant.role);
      expect(restored.activeProducts, _tenant.activeProducts);
    });
  });

  group('TenantContextNotifier', () {
    late FakeSecureStorage storage;
    late TenantContextNotifier notifier;

    setUp(() {
      storage = FakeSecureStorage();
      notifier = TenantContextNotifier(storage: storage);
    });

    test('starts with no tenant and answers no product', () {
      expect(notifier.hasTenant, isFalse);
      expect(notifier.tenantId, isNull);
      expect(notifier.hasProduct(TenantProducts.peopleManagement), isFalse);
    });

    test('keeps the selected tenant available to the whole app', () async {
      await notifier.select(_tenant);

      expect(notifier.hasTenant, isTrue);
      expect(notifier.tenantId, _tenant.id);
      expect(notifier.hasProduct(TenantProducts.peopleManagement), isTrue);
      expect(notifier.hasProduct(TenantProducts.billPayment), isFalse);
    });

    test('restores the previous choice on a fresh launch', () async {
      await notifier.select(_tenant);

      final relaunched = TenantContextNotifier(storage: storage);
      final restored = await relaunched.restore();

      expect(restored?.id, _tenant.id);
      expect(relaunched.tenantId, _tenant.id);
    });

    test('treats an unreadable stored payload as no selection', () async {
      await storage.write(key: 'selected_tenant', value: 'not json at all');

      expect(await notifier.restore(), isNull);
      expect(notifier.hasTenant, isFalse);
      expect(storage.values.containsKey('selected_tenant'), isFalse);
    });

    test('clearing drops the choice in memory and on disk', () async {
      await notifier.select(_tenant);
      await notifier.clear();

      expect(notifier.hasTenant, isFalse);
      expect(await storage.read(key: 'selected_tenant'), isNull);
    });
  });
}
