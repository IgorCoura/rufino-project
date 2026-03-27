import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_personal_info.dart';

void main() {
  group('EmployeePersonalInfo computed properties', () {
    test('hasDisabilities returns true when disabilityIds is not empty', () {
      const info = EmployeePersonalInfo(
        genderId: '1',
        maritalStatusId: '1',
        ethnicityId: '1',
        educationLevelId: '1',
        disabilityIds: ['1'],
        disabilityObservation: '',
      );
      expect(info.hasDisabilities, isTrue);
    });

    test('hasDisabilities returns false when disabilityIds is empty', () {
      const info = EmployeePersonalInfo(
        genderId: '1',
        maritalStatusId: '1',
        ethnicityId: '1',
        educationLevelId: '1',
        disabilityIds: [],
        disabilityObservation: '',
      );
      expect(info.hasDisabilities, isFalse);
    });

    test('isComplete returns true when all main fields are filled', () {
      const info = EmployeePersonalInfo(
        genderId: '1',
        maritalStatusId: '2',
        ethnicityId: '3',
        educationLevelId: '4',
        disabilityIds: [],
        disabilityObservation: '',
      );
      expect(info.isComplete, isTrue);
    });

    test('isComplete returns false when any main field is empty', () {
      const info = EmployeePersonalInfo(
        genderId: '1',
        maritalStatusId: '',
        ethnicityId: '3',
        educationLevelId: '4',
        disabilityIds: [],
        disabilityObservation: '',
      );
      expect(info.isComplete, isFalse);
    });

    test('copyWith overrides only the specified fields', () {
      const info = EmployeePersonalInfo(
        genderId: '1',
        maritalStatusId: '2',
        ethnicityId: '3',
        educationLevelId: '4',
        disabilityIds: [],
        disabilityObservation: '',
      );
      final updated = info.copyWith(genderId: '5');
      expect(updated.genderId, '5');
      expect(updated.maritalStatusId, '2');
    });
  });
}
