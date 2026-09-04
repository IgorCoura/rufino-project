import 'package:flutter_test/flutter_test.dart';
import 'package:people_management/people_management.dart';

void main() {
  DashboardUnitItem itemWith({
    String validity = '',
    String statusId = '2',
    String employeeStatusId = '2',
  }) =>
      DashboardUnitItem(
        documentUnitId: 'unit-1',
        documentId: 'doc-1',
        employeeId: 'emp-1',
        employeeName: 'Maria da Silva',
        employeeStatusId: employeeStatusId,
        employeeStatusName: 'Active',
        documentTemplateName: 'ASO',
        documentGroupName: 'Saúde',
        date: '01/03/2026',
        validity: validity,
        statusId: statusId,
        statusName: 'OK',
        hasFile: true,
      );

  group('DashboardSummary', () {
    const summary = DashboardSummary(
      expired: 3,
      expiring: 2,
      pending: 5,
      awaitingSignature: 1,
      requiresValidation: 4,
    );

    test('countFor returns the count matching each bucket', () {
      expect(summary.countFor(DashboardBucket.expired), 3);
      expect(summary.countFor(DashboardBucket.expiring), 2);
      expect(summary.countFor(DashboardBucket.pending), 5);
      expect(summary.countFor(DashboardBucket.awaitingSignature), 1);
      expect(summary.countFor(DashboardBucket.requiresValidation), 4);
    });

    test('total sums every bucket count', () {
      expect(summary.total, 15);
    });

    test('empty summary has all counts zeroed', () {
      expect(DashboardSummary.empty.total, 0);
    });
  });

  group('DashboardBucket', () {
    test('apiValue matches the backend enum names', () {
      expect(DashboardBucket.expired.apiValue, 'Expired');
      expect(DashboardBucket.expiring.apiValue, 'Expiring');
      expect(DashboardBucket.pending.apiValue, 'Pending');
      expect(DashboardBucket.awaitingSignature.apiValue, 'AwaitingSignature');
      expect(DashboardBucket.requiresValidation.apiValue, 'RequiresValidation');
    });
  });

  group('DashboardUnitItem.daysUntilValidity', () {
    final reference = DateTime(2026, 3, 10, 15, 30);

    test('returns positive days when the validity is in the future', () {
      expect(itemWith(validity: '15/03/2026').daysUntilValidity(reference), 5);
    });

    test('returns zero when the validity is on the reference date', () {
      expect(itemWith(validity: '10/03/2026').daysUntilValidity(reference), 0);
    });

    test('returns negative days when the validity is in the past', () {
      expect(itemWith(validity: '05/03/2026').daysUntilValidity(reference), -5);
    });

    test('returns null when the unit has no validity date', () {
      expect(itemWith().daysUntilValidity(reference), isNull);
    });
  });

  group('DashboardUnitItem.urgencyLabel', () {
    final reference = DateTime(2026, 3, 10);

    test('describes future validity in days', () {
      expect(itemWith(validity: '15/03/2026').urgencyLabel(reference),
          'Vence em 5 dias');
    });

    test('describes validity of tomorrow and today with dedicated phrases',
        () {
      expect(itemWith(validity: '11/03/2026').urgencyLabel(reference),
          'Vence amanhã');
      expect(itemWith(validity: '10/03/2026').urgencyLabel(reference),
          'Vence hoje');
    });

    test('describes overdue validity in days', () {
      expect(itemWith(validity: '09/03/2026').urgencyLabel(reference),
          'Vencido há 1 dia');
      expect(itemWith(validity: '01/03/2026').urgencyLabel(reference),
          'Vencido há 9 dias');
    });

    test('returns null when the unit has no validity date', () {
      expect(itemWith().urgencyLabel(reference), isNull);
    });
  });

  group('DashboardUnitItem labels', () {
    test('statusLabel maps every known unit status id to Portuguese', () {
      expect(itemWith(statusId: '1').statusLabel, 'Pendente');
      expect(itemWith(statusId: '2').statusLabel, 'OK');
      expect(itemWith(statusId: '3').statusLabel, 'Obsoleto');
      expect(itemWith(statusId: '4').statusLabel, 'Inválido');
      expect(itemWith(statusId: '5').statusLabel, 'Requer Validação');
      expect(itemWith(statusId: '6').statusLabel, 'Não Aplicável');
      expect(itemWith(statusId: '7').statusLabel, 'Aguardando Assinatura');
      expect(itemWith(statusId: '8').statusLabel, 'A Vencer');
    });

    test('employeeStatusLabel maps every known employee status id', () {
      expect(itemWith(employeeStatusId: '1').employeeStatusLabel, 'Pendente');
      expect(itemWith(employeeStatusId: '2').employeeStatusLabel, 'Ativo');
      expect(itemWith(employeeStatusId: '3').employeeStatusLabel, 'Férias');
      expect(itemWith(employeeStatusId: '4').employeeStatusLabel, 'Afastado');
      expect(itemWith(employeeStatusId: '5').employeeStatusLabel, 'Inativo');
    });

    test('hasValidity reflects whether a validity date is present', () {
      expect(itemWith(validity: '15/03/2026').hasValidity, isTrue);
      expect(itemWith().hasValidity, isFalse);
    });
  });
}
