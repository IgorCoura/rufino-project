import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_social_integration_program.dart';

void main() {
  group('EmployeeSocialIntegrationProgram computed properties', () {
    test('hasNumber returns true when number is not empty', () {
      const pis = EmployeeSocialIntegrationProgram(number: '07183177441');
      expect(pis.hasNumber, isTrue);
    });

    test('hasNumber returns false when number is empty', () {
      const pis = EmployeeSocialIntegrationProgram(number: '');
      expect(pis.hasNumber, isFalse);
    });
  });

  group('EmployeeSocialIntegrationProgram.isPisValid', () {
    test('returns true for a valid PIS number', () {
      expect(
        EmployeeSocialIntegrationProgram.isPisValid('07183177441'),
        isTrue,
      );
    });

    test('returns true when masked separators are present', () {
      expect(
        EmployeeSocialIntegrationProgram.isPisValid('071.83177.44-1'),
        isTrue,
      );
    });

    test('returns false for wrong length', () {
      expect(EmployeeSocialIntegrationProgram.isPisValid('123'), isFalse);
    });

    test('returns false for all-same-digit numbers', () {
      expect(
        EmployeeSocialIntegrationProgram.isPisValid('11111111111'),
        isFalse,
      );
    });

    test('returns false for an invalid check digit', () {
      expect(
        EmployeeSocialIntegrationProgram.isPisValid('07183177440'),
        isFalse,
      );
    });
  });

  group('EmployeeSocialIntegrationProgram.validateNumber', () {
    test('returns null for a valid PIS', () {
      expect(
        EmployeeSocialIntegrationProgram.validateNumber('071.83177.44-1'),
        isNull,
      );
    });

    test('returns error when empty', () {
      expect(
        EmployeeSocialIntegrationProgram.validateNumber(null),
        isNotNull,
      );
      expect(
        EmployeeSocialIntegrationProgram.validateNumber(''),
        isNotNull,
      );
    });

    test('returns error for wrong length', () {
      expect(
        EmployeeSocialIntegrationProgram.validateNumber('12345'),
        isNotNull,
      );
    });

    test('returns error for invalid check digit', () {
      expect(
        EmployeeSocialIntegrationProgram.validateNumber('07183177440'),
        isNotNull,
      );
    });
  });

  group('EmployeeSocialIntegrationProgram.formatted', () {
    test('formats 11-digit number as 000.00000.00-0', () {
      const pis = EmployeeSocialIntegrationProgram(number: '07183177441');
      expect(pis.formatted, '071.83177.44-1');
    });

    test('returns raw value when digit count is not 11', () {
      const pis = EmployeeSocialIntegrationProgram(number: '12345');
      expect(pis.formatted, '12345');
    });
  });
}
