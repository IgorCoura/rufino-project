import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/remuneration.dart';

void main() {
  group('Remuneration computed properties', () {
    const rem = Remuneration(
      paymentUnit: PaymentUnit(id: '1', name: 'Mensal'),
      baseSalary: BaseSalary(
        type: SalaryType(id: '1', name: 'BRL'),
        value: '3500.00',
      ),
      description: 'Salário base',
    );

    const emptyRem = Remuneration(
      paymentUnit: PaymentUnit(id: '', name: ''),
      baseSalary: BaseSalary(
        type: SalaryType(id: '', name: ''),
        value: '',
      ),
      description: '',
    );

    test('hasSalary returns true when value is not empty', () {
      expect(rem.hasSalary, isTrue);
      expect(emptyRem.hasSalary, isFalse);
    });

    test('hasDescription returns true when description is not empty', () {
      expect(rem.hasDescription, isTrue);
      expect(emptyRem.hasDescription, isFalse);
    });
  });

  group('BaseSalary computed properties', () {
    test('hasValue returns true when value is not empty', () {
      const salary = BaseSalary(
        type: SalaryType(id: '1', name: 'BRL'),
        value: '3500',
      );
      expect(salary.hasValue, isTrue);
    });

    test('hasValue returns false when value is empty', () {
      const salary = BaseSalary(
        type: SalaryType(id: '1', name: 'BRL'),
        value: '',
      );
      expect(salary.hasValue, isFalse);
    });
  });

  group('Remuneration.validateSalaryValue', () {
    test('returns error when null or empty', () {
      expect(Remuneration.validateSalaryValue(null), isNotNull);
      expect(Remuneration.validateSalaryValue(''), isNotNull);
    });

    test('returns error for invalid format', () {
      expect(Remuneration.validateSalaryValue('abc'), isNotNull);
      expect(Remuneration.validateSalaryValue('12.345'), isNotNull);
    });

    test('returns null for valid integer value', () {
      expect(Remuneration.validateSalaryValue('3500'), isNull);
    });

    test('returns null for valid decimal value', () {
      expect(Remuneration.validateSalaryValue('3500.50'), isNull);
    });

    test('accepts comma as decimal separator', () {
      expect(Remuneration.validateSalaryValue('3500,50'), isNull);
    });
  });

  group('Remuneration.validateDescription', () {
    test('returns error when null or empty', () {
      expect(Remuneration.validateDescription(null), isNotNull);
    });

    test('returns error when exceeds 2000 characters', () {
      expect(Remuneration.validateDescription('A' * 2001), isNotNull);
    });

    test('returns null for valid description', () {
      expect(Remuneration.validateDescription('Salário base CLT'), isNull);
    });
  });

  group('Remuneration.validateDropdownSelection', () {
    test('returns error when null or empty', () {
      expect(Remuneration.validateDropdownSelection(null), isNotNull);
      expect(Remuneration.validateDropdownSelection(''), isNotNull);
    });

    test('returns null when value is provided', () {
      expect(Remuneration.validateDropdownSelection('1'), isNull);
    });
  });
}
