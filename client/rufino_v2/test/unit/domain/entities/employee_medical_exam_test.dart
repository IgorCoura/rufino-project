import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_medical_exam.dart';

void main() {
  group('EmployeeMedicalExam computed properties', () {
    test('hasExamDate returns true when dateExam is not empty', () {
      const exam = EmployeeMedicalExam(
        dateExam: '01/01/2026',
        validityExam: '',
      );
      expect(exam.hasExamDate, isTrue);
    });

    test('hasExamDate returns false when dateExam is empty', () {
      const exam = EmployeeMedicalExam(dateExam: '', validityExam: '');
      expect(exam.hasExamDate, isFalse);
    });

    test('isExpired returns true when validity date is in the past', () {
      const exam = EmployeeMedicalExam(
        dateExam: '01/01/2020',
        validityExam: '01/01/2021',
      );
      expect(exam.isExpired, isTrue);
    });

    test('isExpired returns false when validity date is in the future', () {
      const exam = EmployeeMedicalExam(
        dateExam: '01/01/2026',
        validityExam: '01/01/2099',
      );
      expect(exam.isExpired, isFalse);
    });

    test('isExpired returns false when validity is empty', () {
      const exam = EmployeeMedicalExam(dateExam: '01/01/2026', validityExam: '');
      expect(exam.isExpired, isFalse);
    });

    test('isExpired returns false for unparseable validity', () {
      const exam = EmployeeMedicalExam(
        dateExam: '01/01/2026',
        validityExam: 'invalid',
      );
      expect(exam.isExpired, isFalse);
    });
  });

  group('EmployeeMedicalExam.validateDateExam', () {
    test('returns error when empty', () {
      expect(EmployeeMedicalExam.validateDateExam(null), isNotNull);
      expect(EmployeeMedicalExam.validateDateExam(''), isNotNull);
    });

    test('returns error for wrong digit count', () {
      expect(EmployeeMedicalExam.validateDateExam('01/01'), isNotNull);
    });

    test('returns error for date older than 365 days', () {
      expect(EmployeeMedicalExam.validateDateExam('01/01/2020'), isNotNull);
    });

    test('returns error for future date beyond 1 day', () {
      expect(EmployeeMedicalExam.validateDateExam('01/01/2099'), isNotNull);
    });

    test('returns null for a recent valid date', () {
      final now = DateTime.now();
      final day = now.day.toString().padLeft(2, '0');
      final month = now.month.toString().padLeft(2, '0');
      final year = now.year.toString();
      expect(
        EmployeeMedicalExam.validateDateExam('$day/$month/$year'),
        isNull,
      );
    });
  });

  group('EmployeeMedicalExam.validateExamValidity', () {
    test('returns error when empty', () {
      expect(EmployeeMedicalExam.validateExamValidity(null), isNotNull);
      expect(EmployeeMedicalExam.validateExamValidity(''), isNotNull);
    });

    test('returns error for past date', () {
      expect(
        EmployeeMedicalExam.validateExamValidity('01/01/2020'),
        isNotNull,
      );
    });

    test('returns error for date more than 10 years in the future', () {
      expect(
        EmployeeMedicalExam.validateExamValidity('01/01/2099'),
        isNotNull,
      );
    });

    test('returns null for a future date within 10 years', () {
      final future = DateTime.now().add(const Duration(days: 365));
      final day = future.day.toString().padLeft(2, '0');
      final month = future.month.toString().padLeft(2, '0');
      final year = future.year.toString();
      expect(
        EmployeeMedicalExam.validateExamValidity('$day/$month/$year'),
        isNull,
      );
    });
  });
}
