import 'package:bill_payment/src/data/payee_api_models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PayeeMapper', () {
    test('maps the full read model', () {
      final payee = PayeeMapper.fromJson({
        'id': 'payee-1',
        'legalName': 'EDP SAO PAULO SA',
        'taxId': '02.302.100/0001-06',
        'taxIdKind': 'CNPJ',
        'aliases': ['EDP', 'BANDEIRANTE'],
        'acceptedBanks': ['033', '341'],
        'amountPolicy': {
          'kind': 'Range',
          'expectedAmount': null,
          'tolerancePercent': null,
          'minAmount': 100.0,
          'maxAmount': 900.5,
          'isConclusive': true,
        },
        'isActive': true,
      });

      expect(payee.id, 'payee-1');
      expect(payee.taxIdKind, 'CNPJ');
      expect(payee.aliases, ['EDP', 'BANDEIRANTE']);
      expect(payee.acceptedBanks, ['033', '341']);
      expect(payee.amountPolicy.kind, 'Range');
      expect(payee.amountPolicy.minAmount, 100.0);
      expect(payee.amountPolicy.maxAmount, 900.5);
      expect(payee.amountPolicy.isConclusive, isTrue);
      expect(payee.isActive, isTrue);
    });

    test('tolerates missing collections and policy', () {
      final payee = PayeeMapper.fromJson({
        'id': 'payee-2',
        'legalName': 'DAE',
        'taxId': '111.444.777-35',
        'taxIdKind': 'CPF',
      });

      expect(payee.aliases, isEmpty);
      expect(payee.acceptedBanks, isEmpty);
      expect(payee.amountPolicy.isConclusive, isFalse);
    });
  });

  group('PayeePageMapper', () {
    test('maps items and carries the cursor through', () {
      final page = PayeePageMapper.fromJson({
        'items': [
          {
            'id': 'payee-1',
            'legalName': 'EDP',
            'taxId': '02.302.100/0001-06',
            'taxIdKind': 'CNPJ',
          },
        ],
        'nextCursor': 'abc123',
      });

      expect(page.items, hasLength(1));
      expect(page.nextCursor, 'abc123');
      expect(page.hasMore, isTrue);
    });

    test('a null cursor means the last page', () {
      final page = PayeePageMapper.fromJson({'items': [], 'nextCursor': null});

      expect(page.hasMore, isFalse);
    });
  });
}
