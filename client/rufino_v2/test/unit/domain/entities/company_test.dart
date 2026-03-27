import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company.dart';

void main() {
  group('Company computed properties', () {
    const company = Company(
      id: '1',
      corporateName: 'Rufino Ltda',
      fantasyName: 'Rufino',
      cnpj: '12345678000190',
    );

    const noFantasy = Company(
      id: '2',
      corporateName: 'Corp Name',
      fantasyName: '',
      cnpj: '',
    );

    test('hasFantasyName returns true when fantasy name is not empty', () {
      expect(company.hasFantasyName, isTrue);
      expect(noFantasy.hasFantasyName, isFalse);
    });

    test('displayName prefers fantasyName, falls back to corporateName', () {
      expect(company.displayName, 'Rufino');
      expect(noFantasy.displayName, 'Corp Name');
    });
  });

  group('Company.formattedCnpj', () {
    test('formats 14-digit CNPJ as XX.XXX.XXX/XXXX-XX', () {
      const company = Company(
        id: '1',
        corporateName: '',
        fantasyName: '',
        cnpj: '12345678000190',
      );
      expect(company.formattedCnpj, '12.345.678/0001-90');
    });

    test('returns raw value when digit count is not 14', () {
      const company = Company(
        id: '1',
        corporateName: '',
        fantasyName: '',
        cnpj: '123',
      );
      expect(company.formattedCnpj, '123');
    });
  });

  group('Company.validateRequired', () {
    test('returns error when null or empty', () {
      expect(Company.validateRequired(null), isNotNull);
      expect(Company.validateRequired(''), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Company.validateRequired('Rufino'), isNull);
    });
  });

  group('Company.validateCnpj', () {
    test('returns error when null or empty', () {
      expect(Company.validateCnpj(null), isNotNull);
      expect(Company.validateCnpj(''), isNotNull);
    });

    test('returns error when digit count is not 14', () {
      expect(Company.validateCnpj('123'), isNotNull);
    });

    test('returns null for 14-digit CNPJ with mask', () {
      expect(Company.validateCnpj('12.345.678/0001-90'), isNull);
    });

    test('returns null for 14-digit CNPJ without mask', () {
      expect(Company.validateCnpj('12345678000190'), isNull);
    });
  });

  group('Company.validateEmail', () {
    test('returns error when null or empty', () {
      expect(Company.validateEmail(null), isNotNull);
      expect(Company.validateEmail(''), isNotNull);
    });

    test('returns error for invalid email', () {
      expect(Company.validateEmail('notanemail'), isNotNull);
    });

    test('returns null for valid email', () {
      expect(Company.validateEmail('user@example.com'), isNull);
    });
  });
}
