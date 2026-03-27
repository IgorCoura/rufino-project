import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee.dart';

void main() {
  group('EmployeeStatus', () {
    test('fromId returns the correct status for each known id', () {
      expect(EmployeeStatus.fromId(0), EmployeeStatus.none);
      expect(EmployeeStatus.fromId(1), EmployeeStatus.pending);
      expect(EmployeeStatus.fromId(2), EmployeeStatus.active);
      expect(EmployeeStatus.fromId(3), EmployeeStatus.vacation);
      expect(EmployeeStatus.fromId(4), EmployeeStatus.away);
      expect(EmployeeStatus.fromId(5), EmployeeStatus.inactive);
    });

    test('fromId returns none for an unknown id', () {
      expect(EmployeeStatus.fromId(99), EmployeeStatus.none);
    });
  });

  group('DocumentStatus', () {
    test('fromId returns the correct status for each known id', () {
      expect(DocumentStatus.fromId(-1), DocumentStatus.all);
      expect(DocumentStatus.fromId(0), DocumentStatus.ok);
      expect(DocumentStatus.fromId(1), DocumentStatus.expiringSoon);
      expect(DocumentStatus.fromId(2), DocumentStatus.requiresAttention);
    });

    test('fromId returns all for an unknown id', () {
      expect(DocumentStatus.fromId(99), DocumentStatus.all);
    });
  });

  group('Employee', () {
    const active = Employee(
      id: '1',
      name: 'João Silva',
      registration: '001',
      status: EmployeeStatus.active,
      roleName: 'Analista',
      documentStatus: DocumentStatus.ok,
    );

    const inactive = Employee(
      id: '2',
      name: 'Maria Santos',
      registration: '002',
      status: EmployeeStatus.inactive,
      roleName: '',
      documentStatus: DocumentStatus.requiresAttention,
    );

    const pending = Employee(
      id: '3',
      name: 'Carlos Lima',
      registration: '003',
      status: EmployeeStatus.pending,
      roleName: 'Dev',
      documentStatus: DocumentStatus.expiringSoon,
    );

    test('isActive returns true only for active status', () {
      expect(active.isActive, isTrue);
      expect(inactive.isActive, isFalse);
      expect(pending.isActive, isFalse);
    });

    test('isInactive returns true only for inactive status', () {
      expect(inactive.isInactive, isTrue);
      expect(active.isInactive, isFalse);
    });

    test('isPending returns true only for pending status', () {
      expect(pending.isPending, isTrue);
      expect(active.isPending, isFalse);
    });

    test('hasRole returns true when roleName is not empty', () {
      expect(active.hasRole, isTrue);
      expect(inactive.hasRole, isFalse);
    });

    test('documentsRequireAttention returns true for requiresAttention status',
        () {
      expect(inactive.documentsRequireAttention, isTrue);
      expect(active.documentsRequireAttention, isFalse);
    });

    test('documentsExpiringSoon returns true for expiringSoon status', () {
      expect(pending.documentsExpiringSoon, isTrue);
      expect(active.documentsExpiringSoon, isFalse);
    });
  });

  group('Employee.validateName', () {
    test('returns error when value is null', () {
      expect(Employee.validateName(null), isNotNull);
    });

    test('returns error when value is empty', () {
      expect(Employee.validateName(''), isNotNull);
      expect(Employee.validateName('   '), isNotNull);
    });

    test('returns error when name has only one word', () {
      expect(Employee.validateName('João'), isNotNull);
    });

    test('returns error when name exceeds 100 characters', () {
      final longName = '${'A' * 50} ${'B' * 50}';
      expect(Employee.validateName(longName), isNotNull);
    });

    test('returns null for a valid full name', () {
      expect(Employee.validateName('João Silva'), isNull);
      expect(Employee.validateName('Maria de Souza Santos'), isNull);
    });
  });
}
