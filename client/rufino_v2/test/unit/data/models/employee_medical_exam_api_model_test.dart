import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_medical_exam_api_model.dart';

void main() {
  group('EmployeeMedicalExamApiModel', () {
    test('fromJson parses dateExam and validityExam from the GET response', () {
      final json = <String, dynamic>{
        'dateExam': '2026-01-15',
        'validityExam': '2027-01-15',
      };

      final model = EmployeeMedicalExamApiModel.fromJson(json);

      expect(model.dateExam, '2026-01-15');
      expect(model.validityExam, '2027-01-15');
    });

    test('fromJson defaults missing fields to empty strings', () {
      final json = <String, dynamic>{};

      final model = EmployeeMedicalExamApiModel.fromJson(json);

      expect(model.dateExam, '');
      expect(model.validityExam, '');
    });

    test('toEntity converts API dates to dd/MM/yyyy display format', () {
      const model = EmployeeMedicalExamApiModel(
        dateExam: '2026-01-15',
        validityExam: '2027-01-15',
      );

      final entity = model.toEntity();

      expect(entity.dateExam, '15/01/2026');
      expect(entity.validityExam, '15/01/2027');
    });

    test('toEntity handles ISO dates with time component', () {
      const model = EmployeeMedicalExamApiModel(
        dateExam: '2026-03-20T00:00:00',
        validityExam: '2027-03-20T00:00:00',
      );

      final entity = model.toEntity();

      expect(entity.dateExam, '20/03/2026');
      expect(entity.validityExam, '20/03/2027');
    });

    test('toEntity returns empty strings when API dates are empty', () {
      const model = EmployeeMedicalExamApiModel(
        dateExam: '',
        validityExam: '',
      );

      final entity = model.toEntity();

      expect(entity.dateExam, '');
      expect(entity.validityExam, '');
    });

    test('dateToApi converts dd/MM/yyyy to yyyy-MM-dd', () {
      expect(EmployeeMedicalExamApiModel.dateToApi('15/01/2026'),
          '2026-01-15');
      expect(EmployeeMedicalExamApiModel.dateToApi('20/03/2027'),
          '2027-03-20');
    });

    test('toJsonMap produces the expected PUT body shape with API dates', () {
      final json = EmployeeMedicalExamApiModel.toJsonMap(
        'emp-1',
        '15/01/2026',
        '15/01/2027',
      );

      expect(json, {
        'employeeId': 'emp-1',
        'dateExam': '2026-01-15',
        'validityExam': '2027-01-15',
      });
    });
  });
}
