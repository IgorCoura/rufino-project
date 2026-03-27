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

  group('EmployeeContractInfo.validateDate', () {
    test('returns error when empty', () {
      expect(EmployeeContractInfo.validateDate(null), isNotNull);
      expect(EmployeeContractInfo.validateDate(''), isNotNull);
    });

    test('returns error for wrong digit count', () {
      expect(EmployeeContractInfo.validateDate('01/01'), isNotNull);
    });

    test('returns error for date more than 365 days in the past', () {
      expect(EmployeeContractInfo.validateDate('01/01/2020'), isNotNull);
    });

    test('returns error for date more than 365 days in the future', () {
      expect(EmployeeContractInfo.validateDate('01/01/2099'), isNotNull);
    });

    test('returns null for today', () {
      final now = DateTime.now();
      final day = now.day.toString().padLeft(2, '0');
      final month = now.month.toString().padLeft(2, '0');
      final year = now.year.toString();
      expect(EmployeeContractInfo.validateDate('$day/$month/$year'), isNull);
    });
  });
}
