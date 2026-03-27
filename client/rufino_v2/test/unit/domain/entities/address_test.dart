import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/address.dart';

void main() {
  group('Address computed properties', () {
    test('minimal returns formatted summary', () {
      const address = Address(
        zipCode: '01310100',
        street: 'Av. Paulista',
        number: '1000',
        complement: '',
        neighborhood: 'Bela Vista',
        city: 'São Paulo',
        state: 'SP',
        country: 'Brasil',
      );
      expect(address.minimal, 'Bela Vista, São Paulo - SP');
    });

    test('isComplete returns true when all main fields are filled', () {
      const address = Address(
        zipCode: '01310100',
        street: 'Av. Paulista',
        number: '1000',
        complement: '',
        neighborhood: 'Bela Vista',
        city: 'São Paulo',
        state: 'SP',
        country: 'Brasil',
      );
      expect(address.isComplete, isTrue);
    });

    test('isComplete returns false when any main field is empty', () {
      const address = Address(
        zipCode: '',
        street: 'Av. Paulista',
        number: '1000',
        complement: '',
        neighborhood: 'Bela Vista',
        city: 'São Paulo',
        state: 'SP',
        country: 'Brasil',
      );
      expect(address.isComplete, isFalse);
    });
  });

  group('Address.validateCep', () {
    test('returns error when empty', () {
      expect(Address.validateCep(null), isNotNull);
      expect(Address.validateCep(''), isNotNull);
    });

    test('returns error for wrong digit count', () {
      expect(Address.validateCep('1234'), isNotNull);
      expect(Address.validateCep('123456789'), isNotNull);
    });

    test('returns null for valid CEP with mask', () {
      expect(Address.validateCep('01310-100'), isNull);
    });

    test('returns null for valid CEP without mask', () {
      expect(Address.validateCep('01310100'), isNull);
    });
  });

  group('Address.validateRequired', () {
    test('returns error with label when empty', () {
      expect(Address.validateRequired(null, 'Rua'), 'Rua é obrigatório');
      expect(Address.validateRequired('', 'Bairro'), 'Bairro é obrigatório');
    });

    test('returns null when value is filled', () {
      expect(Address.validateRequired('Av. Paulista', 'Rua'), isNull);
    });
  });

  group('Address.validateState', () {
    test('returns null when empty (optional)', () {
      expect(Address.validateState(null), isNull);
      expect(Address.validateState(''), isNull);
    });

    test('returns error for wrong length', () {
      expect(Address.validateState('S'), isNotNull);
      expect(Address.validateState('SPP'), isNotNull);
    });

    test('returns null for valid 2-letter state', () {
      expect(Address.validateState('SP'), isNull);
    });
  });

  group('Address.formattedZipCode', () {
    test('formats 8-digit zip code as XXXXX-XXX', () {
      const address = Address(
        zipCode: '01310100',
        street: '',
        number: '',
        complement: '',
        neighborhood: '',
        city: '',
        state: '',
        country: '',
      );
      expect(address.formattedZipCode, '01310-100');
    });

    test('returns raw value for non-8-digit zip code', () {
      const address = Address(
        zipCode: '1234',
        street: '',
        number: '',
        complement: '',
        neighborhood: '',
        city: '',
        state: '',
        country: '',
      );
      expect(address.formattedZipCode, '1234');
    });
  });

  group('Address.formattedDisplay', () {
    test('builds multi-line output with all parts present', () {
      const address = Address(
        zipCode: '01310100',
        street: 'Av. Paulista',
        number: '1000',
        complement: 'Sala 101',
        neighborhood: 'Bela Vista',
        city: 'São Paulo',
        state: 'SP',
        country: 'Brasil',
      );
      expect(
        address.formattedDisplay,
        'Av. Paulista, 1000 — Sala 101\n'
        'Bela Vista\n'
        'São Paulo — SP, 01310-100\n'
        'Brasil',
      );
    });

    test('skips empty parts gracefully', () {
      const address = Address(
        zipCode: '',
        street: 'Rua A',
        number: '',
        complement: '',
        neighborhood: '',
        city: 'Curitiba',
        state: '',
        country: '',
      );
      expect(
        address.formattedDisplay,
        'Rua A\n'
        'Curitiba',
      );
    });
  });

  group('Address.inlineSummary', () {
    test('joins all parts with commas and appends CEP', () {
      const address = Address(
        zipCode: '01310100',
        street: 'Av. Paulista',
        number: '1000',
        complement: 'Sala 101',
        neighborhood: 'Bela Vista',
        city: 'São Paulo',
        state: 'SP',
        country: 'Brasil',
      );
      expect(
        address.inlineSummary,
        'Av. Paulista, 1000, Sala 101, Bela Vista, São Paulo, SP, Brasil'
        ' - CEP: 01310100',
      );
    });

    test('skips empty parts and omits CEP suffix when zip is empty', () {
      const address = Address(
        zipCode: '',
        street: 'Rua A',
        number: '',
        complement: '',
        neighborhood: '',
        city: 'Curitiba',
        state: '',
        country: '',
      );
      expect(address.inlineSummary, 'Rua A, Curitiba');
    });
  });

  group('Address.validateStreet', () {
    test('returns error when empty', () {
      expect(Address.validateStreet(null), isNotNull);
      expect(Address.validateStreet(''), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(Address.validateStreet('A' * 101), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateStreet('Av. Paulista'), isNull);
    });
  });

  group('Address.validateNumber', () {
    test('returns error when empty', () {
      expect(Address.validateNumber(null), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateNumber('123'), isNull);
    });
  });

  group('Address.validateComplement', () {
    test('returns null for null or empty (optional)', () {
      expect(Address.validateComplement(null), isNull);
      expect(Address.validateComplement(''), isNull);
    });

    test('returns error when exceeds 50 characters', () {
      expect(Address.validateComplement('A' * 51), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateComplement('Sala 101'), isNull);
    });
  });

  group('Address.validateNeighborhood', () {
    test('returns error when empty', () {
      expect(Address.validateNeighborhood(null), isNotNull);
    });

    test('returns error when exceeds 50 characters', () {
      expect(Address.validateNeighborhood('A' * 51), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateNeighborhood('Bela Vista'), isNull);
    });
  });

  group('Address.validateCity', () {
    test('returns error when empty', () {
      expect(Address.validateCity(null), isNotNull);
    });

    test('returns error when exceeds 50 characters', () {
      expect(Address.validateCity('A' * 51), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateCity('São Paulo'), isNull);
    });
  });

  group('Address.validateStateFull', () {
    test('returns error when empty', () {
      expect(Address.validateStateFull(null), isNotNull);
    });

    test('returns error when exceeds 50 characters', () {
      expect(Address.validateStateFull('A' * 51), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateStateFull('SP'), isNull);
    });
  });

  group('Address.validateCountry', () {
    test('returns error when empty', () {
      expect(Address.validateCountry(null), isNotNull);
    });

    test('returns error when exceeds 50 characters', () {
      expect(Address.validateCountry('A' * 51), isNotNull);
    });

    test('returns null for valid value', () {
      expect(Address.validateCountry('Brasil'), isNull);
    });
  });
}
