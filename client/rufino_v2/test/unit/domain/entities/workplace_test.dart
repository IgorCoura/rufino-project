import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/address.dart';
import 'package:rufino_v2/domain/entities/workplace.dart';

void main() {
  const fullAddress = Address(
    zipCode: '01310100',
    street: 'Av. Paulista',
    number: '1000',
    complement: '',
    neighborhood: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
    country: 'Brasil',
  );

  const emptyAddress = Address(
    zipCode: '',
    street: '',
    number: '',
    complement: '',
    neighborhood: '',
    city: '',
    state: '',
    country: '',
  );

  group('Workplace computed properties', () {
    test('hasName returns true when name is not empty', () {
      const wp = Workplace(id: '1', name: 'Sede', address: fullAddress);
      expect(wp.hasName, isTrue);
    });

    test('hasName returns false when name is empty', () {
      const wp = Workplace(id: '1', name: '', address: fullAddress);
      expect(wp.hasName, isFalse);
    });

    test('hasAddress returns true when address is complete', () {
      const wp = Workplace(id: '1', name: 'Sede', address: fullAddress);
      expect(wp.hasAddress, isTrue);
    });

    test('hasAddress returns false when address is incomplete', () {
      const wp = Workplace(id: '1', name: 'Sede', address: emptyAddress);
      expect(wp.hasAddress, isFalse);
    });
  });

  group('Workplace.validateName', () {
    test('returns error when null or empty', () {
      expect(Workplace.validateName(null), isNotNull);
      expect(Workplace.validateName(''), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(Workplace.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(Workplace.validateName('Sede Principal'), isNull);
    });
  });
}
