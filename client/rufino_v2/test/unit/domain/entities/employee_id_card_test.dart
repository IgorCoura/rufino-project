import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_id_card.dart';

void main() {
  group('EmployeeIdCard computed properties', () {
    test('hasCpf returns true when CPF is not empty', () {
      const card = EmployeeIdCard(
        cpf: '12345678909',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: '',
        birthState: '',
        nationality: '',
      );
      expect(card.hasCpf, isTrue);
    });

    test('hasCpf returns false when CPF is empty', () {
      const card = EmployeeIdCard(
        cpf: '',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: '',
        birthState: '',
        nationality: '',
      );
      expect(card.hasCpf, isFalse);
    });
  });

  group('EmployeeIdCard.isCpfValid', () {
    test('returns true for a known valid CPF', () {
      expect(EmployeeIdCard.isCpfValid('529.982.247-25'), isTrue);
      expect(EmployeeIdCard.isCpfValid('52998224725'), isTrue);
    });

    test('returns false for all-same-digit CPFs', () {
      expect(EmployeeIdCard.isCpfValid('11111111111'), isFalse);
      expect(EmployeeIdCard.isCpfValid('00000000000'), isFalse);
    });

    test('returns false for wrong length', () {
      expect(EmployeeIdCard.isCpfValid('123'), isFalse);
      expect(EmployeeIdCard.isCpfValid('1234567890123'), isFalse);
    });

    test('returns false for an invalid check digit', () {
      expect(EmployeeIdCard.isCpfValid('52998224726'), isFalse);
    });
  });

  group('EmployeeIdCard.validateCpf', () {
    test('returns error when empty', () {
      expect(EmployeeIdCard.validateCpf(null), isNotNull);
      expect(EmployeeIdCard.validateCpf(''), isNotNull);
    });

    test('returns error when too long', () {
      final longValue = 'a' * 101;
      expect(EmployeeIdCard.validateCpf(longValue), isNotNull);
    });

    test('returns error for invalid CPF', () {
      expect(EmployeeIdCard.validateCpf('12345678900'), isNotNull);
    });

    test('returns null for valid CPF', () {
      expect(EmployeeIdCard.validateCpf('529.982.247-25'), isNull);
    });
  });

  group('EmployeeIdCard.validateDateOfBirth', () {
    test('returns error when empty', () {
      expect(EmployeeIdCard.validateDateOfBirth(null), isNotNull);
      expect(EmployeeIdCard.validateDateOfBirth(''), isNotNull);
    });

    test('returns error for invalid format', () {
      expect(EmployeeIdCard.validateDateOfBirth('abc'), isNotNull);
      expect(EmployeeIdCard.validateDateOfBirth('1/1/2000'), isNotNull);
    });

    test('returns error for future date', () {
      expect(EmployeeIdCard.validateDateOfBirth('01/01/2099'), isNotNull);
    });

    test('returns error for date older than 100 years', () {
      expect(EmployeeIdCard.validateDateOfBirth('01/01/1900'), isNotNull);
    });

    test('returns null for a valid date of birth', () {
      expect(EmployeeIdCard.validateDateOfBirth('15/06/1990'), isNull);
    });
  });

  group('EmployeeIdCard.validateMotherName', () {
    test('returns error when empty', () {
      expect(EmployeeIdCard.validateMotherName(null), isNotNull);
      expect(EmployeeIdCard.validateMotherName(''), isNotNull);
    });

    test('returns error when too long', () {
      final longName = 'A' * 101;
      expect(EmployeeIdCard.validateMotherName(longName), isNotNull);
    });

    test('returns null for valid name', () {
      expect(EmployeeIdCard.validateMotherName('Maria Silva'), isNull);
    });
  });

  group('EmployeeIdCard.validateFatherName', () {
    test('returns null when null', () {
      expect(EmployeeIdCard.validateFatherName(null), isNull);
    });

    test('returns null when empty', () {
      expect(EmployeeIdCard.validateFatherName(''), isNull);
    });

    test('returns error when too long', () {
      expect(EmployeeIdCard.validateFatherName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(EmployeeIdCard.validateFatherName('José Silva'), isNull);
    });
  });

  group('EmployeeIdCard.validateBirthCity', () {
    test('returns error when empty', () {
      expect(EmployeeIdCard.validateBirthCity(null), isNotNull);
    });

    test('returns error when too long', () {
      expect(EmployeeIdCard.validateBirthCity('A' * 101), isNotNull);
    });

    test('returns null for valid city', () {
      expect(EmployeeIdCard.validateBirthCity('São Paulo'), isNull);
    });
  });

  group('EmployeeIdCard.validateBirthState', () {
    test('returns error when empty', () {
      expect(EmployeeIdCard.validateBirthState(null), isNotNull);
      expect(EmployeeIdCard.validateBirthState(''), isNotNull);
    });

    test('returns error when not exactly 2 characters', () {
      expect(EmployeeIdCard.validateBirthState('S'), isNotNull);
      expect(EmployeeIdCard.validateBirthState('SPP'), isNotNull);
    });

    test('returns null for valid 2-letter state', () {
      expect(EmployeeIdCard.validateBirthState('SP'), isNull);
    });
  });

  group('EmployeeIdCard.validateNationality', () {
    test('returns error when empty', () {
      expect(EmployeeIdCard.validateNationality(null), isNotNull);
    });

    test('returns error when too long', () {
      expect(EmployeeIdCard.validateNationality('A' * 101), isNotNull);
    });

    test('returns null for valid nationality', () {
      expect(EmployeeIdCard.validateNationality('Brasileira'), isNull);
    });
  });

  group('EmployeeIdCard.formattedCpf', () {
    test('formats 11-digit CPF as XXX.XXX.XXX-XX', () {
      const card = EmployeeIdCard(
        cpf: '12345678910',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: '',
        birthState: '',
        nationality: '',
      );
      expect(card.formattedCpf, '123.456.789-10');
    });

    test('returns raw value when digit count is not 11', () {
      const card = EmployeeIdCard(
        cpf: '12345',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: '',
        birthState: '',
        nationality: '',
      );
      expect(card.formattedCpf, '12345');
    });
  });

  group('EmployeeIdCard.formattedBirthPlace', () {
    test('returns city and state formatted as "City — SP"', () {
      const card = EmployeeIdCard(
        cpf: '',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: 'São Paulo',
        birthState: 'sp',
        nationality: '',
      );
      expect(card.formattedBirthPlace, 'São Paulo — SP');
    });

    test('returns just the city when state is empty', () {
      const card = EmployeeIdCard(
        cpf: '',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: 'Curitiba',
        birthState: '',
        nationality: '',
      );
      expect(card.formattedBirthPlace, 'Curitiba');
    });

    test('returns Não informado when city is empty', () {
      const card = EmployeeIdCard(
        cpf: '',
        motherName: '',
        fatherName: '',
        dateOfBirth: '',
        birthCity: '',
        birthState: 'SP',
        nationality: '',
      );
      expect(card.formattedBirthPlace, 'Não informado');
    });
  });
}
