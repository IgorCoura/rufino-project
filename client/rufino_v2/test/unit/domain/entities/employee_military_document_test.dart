import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_military_document.dart';

void main() {
  group('EmployeeMilitaryDocument computed properties', () {
    test('isExempt returns true when not required', () {
      const doc = EmployeeMilitaryDocument(
        number: '',
        type: '',
        isRequired: false,
      );
      expect(doc.isExempt, isTrue);
    });

    test('isExempt returns false when required', () {
      const doc = EmployeeMilitaryDocument(
        number: '123',
        type: 'Reservista',
        isRequired: true,
      );
      expect(doc.isExempt, isFalse);
    });

    test('hasData returns true when number or type is filled', () {
      const doc = EmployeeMilitaryDocument(
        number: '123',
        type: '',
        isRequired: true,
      );
      expect(doc.hasData, isTrue);
    });

    test('hasData returns false when both are empty', () {
      const doc = EmployeeMilitaryDocument(
        number: '',
        type: '',
        isRequired: false,
      );
      expect(doc.hasData, isFalse);
    });
  });

  group('EmployeeMilitaryDocument.validateNumber', () {
    test('returns error when empty', () {
      expect(EmployeeMilitaryDocument.validateNumber(null), isNotNull);
      expect(EmployeeMilitaryDocument.validateNumber(''), isNotNull);
    });

    test('returns error when exceeds 20 characters', () {
      expect(
        EmployeeMilitaryDocument.validateNumber('A' * 21),
        isNotNull,
      );
    });

    test('returns null for valid number', () {
      expect(EmployeeMilitaryDocument.validateNumber('12345'), isNull);
    });
  });

  group('EmployeeMilitaryDocument.validateType', () {
    test('returns error when empty', () {
      expect(EmployeeMilitaryDocument.validateType(null), isNotNull);
      expect(EmployeeMilitaryDocument.validateType(''), isNotNull);
    });

    test('returns error when exceeds 50 characters', () {
      expect(
        EmployeeMilitaryDocument.validateType('A' * 51),
        isNotNull,
      );
    });

    test('returns null for valid type', () {
      expect(EmployeeMilitaryDocument.validateType('Reservista'), isNull);
    });
  });
}
