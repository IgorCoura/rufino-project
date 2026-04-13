import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_contract.dart';

void main() {
  group('EmployeeContractInfo computed properties', () {
    test('isActive returns true when finalDate is empty', () {
      const contract = EmployeeContractInfo(
        initDate: '01/01/2026',
        finalDate: '',
        typeId: '1',
        typeName: 'CLT',
      );
      expect(contract.isActive, isTrue);
      expect(contract.isTerminated, isFalse);
    });

    test('isTerminated returns true when finalDate is not empty', () {
      const contract = EmployeeContractInfo(
        initDate: '01/01/2026',
        finalDate: '31/12/2026',
        typeId: '1',
        typeName: 'CLT',
      );
      expect(contract.isTerminated, isTrue);
      expect(contract.isActive, isFalse);
    });
  });

  group('EmployeeContractInfo.validateInitDate', () {
    test('returns error when empty', () {
      expect(EmployeeContractInfo.validateInitDate(null), isNotNull);
      expect(EmployeeContractInfo.validateInitDate(''), isNotNull);
    });

    test('returns error for wrong digit count', () {
      expect(EmployeeContractInfo.validateInitDate('01/01'), isNotNull);
    });

    test('returns error for date more than 12 years in the past', () {
      expect(EmployeeContractInfo.validateInitDate('01/01/2010'), isNotNull);
    });

    test('returns error for date more than 1 year in the future', () {
      expect(EmployeeContractInfo.validateInitDate('01/01/2099'), isNotNull);
    });

    test('returns null for today', () {
      final now = DateTime.now();
      final day = now.day.toString().padLeft(2, '0');
      final month = now.month.toString().padLeft(2, '0');
      final year = now.year.toString();
      expect(
          EmployeeContractInfo.validateInitDate('$day/$month/$year'), isNull);
    });
  });

  group('EmployeeContractInfo.validateFinalDate', () {
    test('returns error when empty', () {
      expect(EmployeeContractInfo.validateFinalDate(null), isNotNull);
      expect(EmployeeContractInfo.validateFinalDate(''), isNotNull);
    });

    test('returns error for wrong digit count', () {
      expect(EmployeeContractInfo.validateFinalDate('01/01'), isNotNull);
    });

    test('returns error for date more than 1 year in the past', () {
      expect(EmployeeContractInfo.validateFinalDate('01/01/2020'), isNotNull);
    });

    test('returns error for date more than 1 year in the future', () {
      expect(EmployeeContractInfo.validateFinalDate('01/01/2099'), isNotNull);
    });

    test('returns null for today', () {
      final now = DateTime.now();
      final day = now.day.toString().padLeft(2, '0');
      final month = now.month.toString().padLeft(2, '0');
      final year = now.year.toString();
      expect(
          EmployeeContractInfo.validateFinalDate('$day/$month/$year'), isNull);
    });
  });
}
