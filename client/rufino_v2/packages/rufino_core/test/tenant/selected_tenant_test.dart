import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

void main() {
  group('TenantProducts', () {
    test('spells the product codes exactly as the backend sends them', () {
      expect(TenantProducts.peopleManagement, 'PeopleManagement');
      expect(TenantProducts.billPayment, 'BillPayment');
    });
  });

  group('SelectedTenant read from the API payload', () {
    test('reads every field of a complete payload', () {
      final selected = SelectedTenant.fromJson({
        'id': 'tenant-1',
        'kind': 'Company',
        'legalName': 'Rufino Servicos LTDA',
        'tradeName': 'Rufino',
        'status': 'Active',
        'role': 'Owner',
        'activeProducts': ['PeopleManagement', 'BillPayment'],
      });

      expect(selected.id, 'tenant-1');
      expect(selected.kind, 'Company');
      expect(selected.legalName, 'Rufino Servicos LTDA');
      expect(selected.tradeName, 'Rufino');
      expect(selected.status, 'Active');
      expect(selected.role, 'Owner');
      expect(selected.activeProducts, ['PeopleManagement', 'BillPayment']);
    });

    test('falls back to empty values when optional fields are absent', () {
      final selected = SelectedTenant.fromJson({'id': 'tenant-1'});

      expect(selected.kind, '');
      expect(selected.legalName, '');
      expect(selected.tradeName, '');
      expect(selected.status, '');
      expect(selected.role, '');
      expect(selected.activeProducts, isEmpty);
    });

    test('refuses a payload with no tenant id, since there is no context '
        'without one', () {
      expect(
        () => SelectedTenant.fromJson(const {'kind': 'Company'}),
        throwsA(isA<TypeError>()),
      );
    });

    test('survives a round trip through its persisted form', () {
      const original = SelectedTenant(
        id: 'tenant-1',
        kind: 'Individual',
        legalName: 'Ana Souza',
        tradeName: '',
        status: 'Suspended',
        role: 'Member',
        activeProducts: ['BillPayment'],
      );

      final restored = SelectedTenant.fromJson(original.toJson());

      expect(restored.id, original.id);
      expect(restored.kind, original.kind);
      expect(restored.legalName, original.legalName);
      expect(restored.status, original.status);
      expect(restored.role, original.role);
      expect(restored.activeProducts, original.activeProducts);
    });
  });

  group('SelectedTenant classification', () {
    test('recognizes a natural person by its kind', () {
      expect(SelectedTenant.fromJson({'id': 't', 'kind': 'Individual'})
          .isIndividual, isTrue);
      expect(SelectedTenant.fromJson({'id': 't', 'kind': 'Company'})
          .isIndividual, isFalse);
    });

    test('recognizes a frozen registration by its status', () {
      expect(SelectedTenant.fromJson({'id': 't', 'status': 'Suspended'})
          .isSuspended, isTrue);
      expect(SelectedTenant.fromJson({'id': 't', 'status': 'Active'})
          .isSuspended, isFalse);
    });

    test('recognizes the person who answers for the tenant by their role', () {
      expect(
        SelectedTenant.fromJson({'id': 't', 'role': 'Owner'}).isOwner,
        isTrue,
      );
      expect(
        SelectedTenant.fromJson({'id': 't', 'role': 'Member'}).isOwner,
        isFalse,
      );
    });
  });

  group('SelectedTenant display name', () {
    test('prefers the trade name when the tenant has one', () {
      final selected = SelectedTenant.fromJson({
        'id': 't',
        'legalName': 'Rufino Servicos LTDA',
        'tradeName': 'Rufino',
      });

      expect(selected.displayName, 'Rufino');
    });

    test('falls back to the legal name for an individual', () {
      final selected = SelectedTenant.fromJson({
        'id': 't',
        'legalName': 'Ana Souza',
        'tradeName': '',
      });

      expect(selected.displayName, 'Ana Souza');
    });
  });

  group('SelectedTenant products', () {
    test('confirms a product the tenant bought and denies one it did not', () {
      final selected = SelectedTenant.fromJson({
        'id': 't',
        'activeProducts': ['BillPayment'],
      });

      expect(selected.hasProduct(TenantProducts.billPayment), isTrue);
      expect(selected.hasProduct(TenantProducts.peopleManagement), isFalse);
    });

    test('denies every product when the tenant has none enabled', () {
      final selected = SelectedTenant.fromJson({'id': 't'});

      expect(selected.hasProduct(TenantProducts.billPayment), isFalse);
    });

    test('matches the product code exactly, so a typo hides the product', () {
      final selected = SelectedTenant.fromJson({
        'id': 't',
        'activeProducts': ['BillPayment'],
      });

      expect(selected.hasProduct('billpayment'), isFalse);
    });

    test('exposes the product list as read-only', () {
      final selected = SelectedTenant.fromJson({
        'id': 't',
        'activeProducts': ['BillPayment'],
      });

      expect(
        () => selected.activeProducts.add('PeopleManagement'),
        throwsUnsupportedError,
      );
    });
  });
}
