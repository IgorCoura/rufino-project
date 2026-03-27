import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_dependent.dart';

void main() {
  group('EmployeeDependent computed properties', () {
    const newDependent = EmployeeDependent(
      originalName: '',
      name: 'Ana',
      genderId: '2',
      dependencyTypeId: '1',
      cpf: '',
      motherName: '',
      fatherName: '',
      dateOfBirth: '',
      birthCity: '',
      birthState: '',
      nationality: '',
    );

    const existingDependent = EmployeeDependent(
      originalName: 'João Jr',
      name: 'João Jr',
      genderId: '1',
      dependencyTypeId: '2',
      cpf: '',
      motherName: '',
      fatherName: '',
      dateOfBirth: '',
      birthCity: '',
      birthState: '',
      nationality: '',
    );

    test('isNew returns true when originalName is empty', () {
      expect(newDependent.isNew, isTrue);
    });

    test('isNew returns false when originalName is set', () {
      expect(existingDependent.isNew, isFalse);
    });

    test('isChild returns true for dependency type 1', () {
      expect(newDependent.isChild, isTrue);
      expect(newDependent.isSpouse, isFalse);
    });

    test('isSpouse returns true for dependency type 2', () {
      expect(existingDependent.isSpouse, isTrue);
      expect(existingDependent.isChild, isFalse);
    });
  });

  group('EmployeeDependent.validateName', () {
    test('returns error when empty', () {
      expect(EmployeeDependent.validateName(null), isNotNull);
      expect(EmployeeDependent.validateName(''), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(EmployeeDependent.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(EmployeeDependent.validateName('Maria Silva'), isNull);
    });
  });

  group('EmployeeDependent.dependencyTypeLabel', () {
    EmployeeDependent dependentWithType(String typeId) => EmployeeDependent(
          originalName: '',
          name: 'Test',
          genderId: '1',
          dependencyTypeId: typeId,
          cpf: '',
          motherName: '',
          fatherName: '',
          dateOfBirth: '',
          birthCity: '',
          birthState: '',
          nationality: '',
        );

    test('returns Filho(a) for dependency type 1', () {
      expect(dependentWithType('1').dependencyTypeLabel, 'Filho(a)');
    });

    test('returns Cônjuge for dependency type 2', () {
      expect(dependentWithType('2').dependencyTypeLabel, 'Cônjuge');
    });

    test('returns raw value for unknown dependency type', () {
      expect(dependentWithType('99').dependencyTypeLabel, '99');
    });
  });
}
