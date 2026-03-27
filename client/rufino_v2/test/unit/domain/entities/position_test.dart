import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/position.dart';

void main() {
  group('Position computed properties', () {
    const pos = Position(
      id: '1',
      name: 'Dev',
      description: 'Desenvolvedor',
      cbo: '212405',
      roles: [],
    );

    test('hasCbo returns true when cbo is not empty', () {
      expect(pos.hasCbo, isTrue);
    });

    test('hasRoles returns false for empty list', () {
      expect(pos.hasRoles, isFalse);
    });

    test('roleCount returns zero for empty list', () {
      expect(pos.roleCount, 0);
    });
  });

  group('Position.validateName', () {
    test('returns error when empty', () {
      expect(Position.validateName(null), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(Position.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(Position.validateName('Desenvolvedor'), isNull);
    });
  });

  group('Position.validateDescription', () {
    test('returns error when empty', () {
      expect(Position.validateDescription(null), isNotNull);
    });

    test('returns error when exceeds 2000 characters', () {
      expect(Position.validateDescription('A' * 2001), isNotNull);
    });

    test('returns null for valid description', () {
      expect(Position.validateDescription('Desenvolvedor de software'), isNull);
    });
  });

  group('Position.validateCbo', () {
    test('returns error when empty', () {
      expect(Position.validateCbo(null), isNotNull);
    });

    test('returns error when exceeds 6 characters', () {
      expect(Position.validateCbo('1234567'), isNotNull);
    });

    test('returns null for valid CBO', () {
      expect(Position.validateCbo('212405'), isNull);
    });
  });
}
