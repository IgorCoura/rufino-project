import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/department.dart';

void main() {
  group('Department computed properties', () {
    const dept = Department(
      id: '1',
      name: 'TI',
      description: 'Tecnologia da Informação',
      positions: [],
    );

    test('hasDescription returns true when description is not empty', () {
      expect(dept.hasDescription, isTrue);
    });

    test('hasPositions returns false for empty list', () {
      expect(dept.hasPositions, isFalse);
    });

    test('positionCount returns zero for empty list', () {
      expect(dept.positionCount, 0);
    });
  });

  group('Department.validateName', () {
    test('returns error when null or empty', () {
      expect(Department.validateName(null), isNotNull);
      expect(Department.validateName(''), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(Department.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(Department.validateName('TI'), isNull);
    });
  });

  group('Department.validateDescription', () {
    test('returns error when null or empty', () {
      expect(Department.validateDescription(null), isNotNull);
    });

    test('returns error when exceeds 2000 characters', () {
      expect(Department.validateDescription('A' * 2001), isNotNull);
    });

    test('returns null for valid description', () {
      expect(Department.validateDescription('Setor de tecnologia'), isNull);
    });
  });
}
