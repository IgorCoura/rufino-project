import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';
import 'package:rufino_v2/domain/entities/role.dart';

void main() {
  group('Role computed properties', () {
    test('hasCbo returns true when cbo is not empty', () {
      expect(_makeRole(cbo: '212405').hasCbo, isTrue);
      expect(_makeRole(cbo: '').hasCbo, isFalse);
    });

    test('hasDescription returns true when description is not empty', () {
      expect(_makeRole(description: 'Analista').hasDescription, isTrue);
      expect(_makeRole(description: '').hasDescription, isFalse);
    });
  });

  group('Role.validateName', () {
    test('returns error when empty', () {
      expect(Role.validateName(null), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(Role.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(Role.validateName('Analista Jr'), isNull);
    });
  });

  group('Role.validateDescription', () {
    test('returns error when empty', () {
      expect(Role.validateDescription(null), isNotNull);
    });

    test('returns error when exceeds 2000 characters', () {
      expect(Role.validateDescription('A' * 2001), isNotNull);
    });

    test('returns null for valid description', () {
      expect(Role.validateDescription('Analista de sistemas'), isNull);
    });
  });

  group('Role.validateCbo', () {
    test('returns error when empty', () {
      expect(Role.validateCbo(null), isNotNull);
    });

    test('returns error when exceeds 6 characters', () {
      expect(Role.validateCbo('1234567'), isNotNull);
    });

    test('returns null for valid CBO', () {
      expect(Role.validateCbo('212405'), isNull);
    });
  });
}

Role _makeRole({String cbo = '', String description = ''}) {
  return Role(
    id: '1',
    name: 'Test',
    description: description,
    cbo: cbo,
    remuneration: const Remuneration(
      paymentUnit: PaymentUnit(id: '1', name: 'Mensal'),
      baseSalary: BaseSalary(
        type: SalaryType(id: '1', name: 'BRL'),
        value: '3500',
      ),
      description: '',
    ),
  );
}
