import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_document.dart';
import 'package:rufino_v2/domain/entities/period.dart';

void main() {
  group('DocumentUnit status getters', () {
    DocumentUnit unitWithStatus(String statusId) => DocumentUnit(
          id: '1',
          statusId: statusId,
          statusName: '',
          date: '',
          validity: '',
          createdAt: '',
          hasFile: false,
          name: '',
        );

    test('isPending returns true only for status 1', () {
      expect(unitWithStatus('1').isPending, isTrue);
      expect(unitWithStatus('2').isPending, isFalse);
    });

    test('isOk returns true only for status 2', () {
      expect(unitWithStatus('2').isOk, isTrue);
    });

    test('isObsolete returns true only for status 3', () {
      expect(unitWithStatus('3').isObsolete, isTrue);
    });

    test('isInvalid returns true only for status 4', () {
      expect(unitWithStatus('4').isInvalid, isTrue);
    });

    test('requiresValidation returns true only for status 5', () {
      expect(unitWithStatus('5').requiresValidation, isTrue);
    });

    test('isNotApplicable returns true only for status 6', () {
      expect(unitWithStatus('6').isNotApplicable, isTrue);
    });

    test('isWaitingSignature returns true only for status 7', () {
      expect(unitWithStatus('7').isWaitingSignature, isTrue);
    });

    test('isWarning returns true only for status 8', () {
      expect(unitWithStatus('8').isWarning, isTrue);
      expect(unitWithStatus('2').isWarning, isFalse);
    });
  });

  group('EmployeeDocument computed properties', () {
    const pending = DocumentUnit(
      id: '1',
      statusId: '1',
      statusName: 'Pendente',
      date: '',
      validity: '',
      createdAt: '',
      hasFile: false,
      name: '',
    );
    const ok = DocumentUnit(
      id: '2',
      statusId: '2',
      statusName: 'OK',
      date: '',
      validity: '',
      createdAt: '',
      hasFile: true,
      name: 'file.pdf',
    );

    test('hasPendingUnits returns true when any unit is pending', () {
      const doc = EmployeeDocument(
        id: '1',
        name: 'Doc',
        description: '',
        statusId: '1',
        statusName: '',
        isSignable: false,
        canGenerateDocument: false,
        usePreviousPeriod: false,
        totalUnitsCount: 2,
        units: [pending, ok],
      );
      expect(doc.hasPendingUnits, isTrue);
    });

    test('hasPendingUnits returns false when no unit is pending', () {
      const doc = EmployeeDocument(
        id: '1',
        name: 'Doc',
        description: '',
        statusId: '2',
        statusName: '',
        isSignable: false,
        canGenerateDocument: false,
        usePreviousPeriod: false,
        totalUnitsCount: 1,
        units: [ok],
      );
      expect(doc.hasPendingUnits, isFalse);
    });

    test('hasFilledUnits returns true when any unit has a file', () {
      const doc = EmployeeDocument(
        id: '1',
        name: 'Doc',
        description: '',
        statusId: '1',
        statusName: '',
        isSignable: false,
        canGenerateDocument: false,
        usePreviousPeriod: false,
        totalUnitsCount: 2,
        units: [pending, ok],
      );
      expect(doc.hasFilledUnits, isTrue);
    });

    test('pendingUnitsCount returns the count of pending units', () {
      const doc = EmployeeDocument(
        id: '1',
        name: 'Doc',
        description: '',
        statusId: '1',
        statusName: '',
        isSignable: false,
        canGenerateDocument: false,
        usePreviousPeriod: false,
        totalUnitsCount: 2,
        units: [pending, ok],
      );
      expect(doc.pendingUnitsCount, 1);
    });
  });

  group('EmployeeDocument.statusLabel', () {
    EmployeeDocument docWithStatus(String id, {String name = ''}) =>
        EmployeeDocument(
          id: '1',
          name: 'Doc',
          description: '',
          statusId: id,
          statusName: name,
          isSignable: false,
          canGenerateDocument: false,
          usePreviousPeriod: false,
          totalUnitsCount: 0,
          units: const [],
        );

    test('reads the document-level scale, not the three-valued rollup', () {
      expect(docWithStatus('1').statusLabel, 'Falta Entregar');
      expect(docWithStatus('2').statusLabel, 'Requer Validação');
      expect(docWithStatus('3').statusLabel, 'OK');
      expect(docWithStatus('4').statusLabel, 'Obsoleto');
      expect(docWithStatus('5').statusLabel, 'Aguardando Assinatura');
      expect(docWithStatus('6').statusLabel, 'A Vencer');
    });

    test('labels a document with expired coverage as Vencido', () {
      expect(docWithStatus('7').statusLabel, 'Vencido');
    });

    test('translates the English enum name when the id does not match', () {
      expect(
        docWithStatus('', name: 'RequiresDocument').statusLabel,
        'Falta Entregar',
      );
      expect(
        docWithStatus('', name: 'AwaitingSignature').statusLabel,
        'Aguardando Assinatura',
      );
    });

    test('returns statusName as fallback for unknown status id', () {
      expect(docWithStatus('99', name: 'Custom').statusLabel, 'Custom');
    });

    test('returns raw status id when statusName is also empty', () {
      expect(docWithStatus('99').statusLabel, '99');
    });
  });

  group('DocumentUnit.statusLabel', () {
    DocumentUnit unitWithStatus(String id, {String name = ''}) => DocumentUnit(
          id: '1',
          statusId: id,
          statusName: name,
          date: '',
          validity: '',
          createdAt: '',
          hasFile: false,
          name: '',
        );

    test('returns Pendente for status id 1', () {
      expect(unitWithStatus('1').statusLabel, 'Pendente');
    });

    test('returns OK for status id 2', () {
      expect(unitWithStatus('2').statusLabel, 'OK');
    });

    test('returns Obsoleto for status id 3', () {
      expect(unitWithStatus('3').statusLabel, 'Obsoleto');
    });

    test('returns Inválido for status id 4', () {
      expect(unitWithStatus('4').statusLabel, 'Inválido');
    });

    test('returns Requer Validação for status id 5', () {
      expect(unitWithStatus('5').statusLabel, 'Requer Validação');
    });

    test('returns Não Aplicável for status id 6', () {
      expect(unitWithStatus('6').statusLabel, 'Não Aplicável');
    });

    test('returns Aguardando Assinatura for status id 7', () {
      expect(unitWithStatus('7').statusLabel, 'Aguardando Assinatura');
    });

    test('returns A Vencer for status id 8', () {
      expect(unitWithStatus('8').statusLabel, 'A Vencer');
    });

    test('returns Vencido for status id 9', () {
      expect(unitWithStatus('9').statusLabel, 'Vencido');
    });

    test('translates the English enum name when the id does not match', () {
      expect(unitWithStatus('', name: 'Pending').statusLabel, 'Pendente');
      expect(
        unitWithStatus('', name: 'AwaitingSignature').statusLabel,
        'Aguardando Assinatura',
      );
      expect(
        unitWithStatus('', name: 'NotApplicable').statusLabel,
        'Não Aplicável',
      );
    });

    test('returns statusName as fallback for unknown status id', () {
      expect(unitWithStatus('99', name: 'Custom').statusLabel, 'Custom');
    });
  });

  group('DocumentUnit status action rules', () {
    DocumentUnit unitWithStatus(String id) => DocumentUnit(
          id: '1',
          statusId: id,
          statusName: '',
          date: '',
          validity: '',
          createdAt: '',
          hasFile: false,
          name: '',
        );

    test('only a document in force can be deprecated', () {
      expect(unitWithStatus('2').canBeDeprecated, isTrue);

      for (final other in ['1', '3', '4', '5', '6', '7', '8', '9']) {
        expect(unitWithStatus(other).canBeDeprecated, isFalse,
            reason: 'status $other should not be deprecatable');
      }
    });

    test('a pending or delivered document can be invalidated', () {
      expect(unitWithStatus('1').canBeInvalidated, isTrue);
      expect(unitWithStatus('2').canBeInvalidated, isTrue);
    });

    // Dispensar o documento é decisão administrativa, não prova de cobertura:
    // desfazê-la quando ele volta a ser exigido não apaga período nenhum, e é
    // a única ação que essa unidade tem.
    test('a not applicable document can be invalidated', () {
      expect(unitWithStatus('6').canBeInvalidated, isTrue);
    });

    // Depreciada e vencida provam que o funcionário esteve coberto no período.
    test('deprecated and expired documents can never be invalidated', () {
      expect(unitWithStatus('3').canBeInvalidated, isFalse);
      expect(unitWithStatus('9').canBeInvalidated, isFalse);
    });

    test('a document in flight or already invalid cannot be invalidated', () {
      for (final other in ['4', '5', '7', '8']) {
        expect(unitWithStatus(other).canBeInvalidated, isFalse,
            reason: 'status $other should not be invalidatable');
      }
    });

    test('only a pending document can be marked not applicable', () {
      expect(unitWithStatus('1').canBeMarkedNotApplicable, isTrue);

      for (final other in ['2', '3', '4', '5', '6', '7', '8', '9']) {
        expect(unitWithStatus(other).canBeMarkedNotApplicable, isFalse,
            reason: 'status $other should not be markable as not applicable');
      }
    });

    test('isExpired reads status id 9', () {
      expect(unitWithStatus('9').isExpired, isTrue);
      expect(unitWithStatus('3').isExpired, isFalse);
    });

    // Renovar troca uma entrega que teve valor pela próxima, então vale antes
    // de vencer (OK, A Vencer) e depois (Vencido).
    test('a document in force or already expired can be renewed', () {
      expect(unitWithStatus('2').canBeRenewed, isTrue);
      expect(unitWithStatus('8').canBeRenewed, isTrue);
      expect(unitWithStatus('9').canBeRenewed, isTrue);
    });

    // Obsoleta já tem substituto; pendente, requer validação e aguardando
    // assinatura são a entrega em curso — ali falta entregar, não renovar.
    test('a document that has nothing to renew cannot be renewed', () {
      for (final other in ['1', '3', '4', '5', '6', '7']) {
        expect(unitWithStatus(other).canBeRenewed, isFalse,
            reason: 'status $other should not be renewable');
      }
    });

    test('a unit is a renewal only when it points to the unit it replaces', () {
      expect(unitWithStatus('1').isRenewal, isFalse);

      const renewal = DocumentUnit(
        id: '2',
        statusId: '1',
        statusName: '',
        date: '',
        validity: '',
        createdAt: '',
        hasFile: false,
        name: '',
        replacesDocumentUnitId: 'unit-1',
      );
      expect(renewal.isRenewal, isTrue);
    });
  });

  group('DocumentUnit.dateForFileName', () {
    test('converts dd/MM/yyyy date to yyyy_MM_dd format', () {
      const unit = DocumentUnit(
        id: '1',
        statusId: '1',
        statusName: '',
        date: '01/03/2026',
        validity: '',
        createdAt: '',
        hasFile: false,
        name: '',
      );
      expect(unit.dateForFileName, '2026_03_01');
    });

    test('returns SEM_DATA when date is empty', () {
      const unit = DocumentUnit(
        id: '1',
        statusId: '1',
        statusName: '',
        date: '',
        validity: '',
        createdAt: '',
        hasFile: false,
        name: '',
      );
      expect(unit.dateForFileName, 'SEM_DATA');
    });
  });

  group('DocumentUnit.downloadFileName', () {
    test('follows backend pattern with employee, date, document, suffix', () {
      const unit = DocumentUnit(
        id: 'abc-unit-1234',
        statusId: '1',
        statusName: '',
        date: '01/03/2026',
        validity: '',
        createdAt: '',
        hasFile: false,
        name: '',
      );
      expect(
        unit.downloadFileName(
          'Contrato de Trabalho',
          employeeName: 'Alice Silva',
        ),
        'ALICE_SILVA-2026_03_01-CONTRATO_DE_TRABALHO-1234.PDF',
      );
    });

    test('uses custom extension when provided', () {
      const unit = DocumentUnit(
        id: 'abc-unit-5678',
        statusId: '1',
        statusName: '',
        date: '15/06/2025',
        validity: '',
        createdAt: '',
        hasFile: false,
        name: '',
      );
      expect(
        unit.downloadFileName(
          'Holerite',
          employeeName: 'Bob Santos',
          extension: 'png',
        ),
        'BOB_SANTOS-2025_06_15-HOLERITE-5678.PNG',
      );
    });
  });

  group('DocumentUnit scheduled signature send', () {
    DocumentUnit unitScheduledOn(String sendOn) => DocumentUnit(
          id: '1',
          statusId: '1',
          statusName: '',
          date: '',
          validity: '',
          createdAt: '',
          hasFile: false,
          name: '',
          scheduledSignatureSendOn: sendOn,
        );

    test('isSignatureScheduled reflects whether a send date is set', () {
      expect(unitScheduledOn('15/03/2026').isSignatureScheduled, isTrue);
      expect(unitScheduledOn('').isSignatureScheduled, isFalse);
    });

    test('a scheduled unit is still pending, since the schedule is an intent',
        () {
      expect(unitScheduledOn('15/03/2026').isPending, isTrue);
    });
  });

  group('DocumentUnit.validateScheduleSendDate', () {
    String todayPlus(int days) {
      final target = DateTime.now().add(Duration(days: days));
      final d = target.day.toString().padLeft(2, '0');
      final m = target.month.toString().padLeft(2, '0');
      return '$d/$m/${target.year}';
    }

    test('returns error when empty', () {
      expect(DocumentUnit.validateScheduleSendDate(''), isNotNull);
    });

    test('returns error for an incomplete date', () {
      expect(DocumentUnit.validateScheduleSendDate('15/03'), isNotNull);
    });

    test('returns error for a date in the past, which the API rejects', () {
      expect(DocumentUnit.validateScheduleSendDate(todayPlus(-1)), isNotNull);
    });

    test('accepts today, since the send goes out on the same day', () {
      expect(DocumentUnit.validateScheduleSendDate(todayPlus(0)), isNull);
    });

    test('accepts a future date', () {
      expect(DocumentUnit.validateScheduleSendDate(todayPlus(30)), isNull);
    });
  });

  group('DocumentUnit.validateSignDeadline', () {
    test('returns error when empty', () {
      expect(DocumentUnit.validateSignDeadline('', '15/03/2026'), isNotNull);
    });

    test('returns error when it is the same day as the send', () {
      expect(
        DocumentUnit.validateSignDeadline('15/03/2026', '15/03/2026'),
        isNotNull,
      );
    });

    test('returns error when it is before the send', () {
      expect(
        DocumentUnit.validateSignDeadline('14/03/2026', '15/03/2026'),
        isNotNull,
      );
    });

    test('accepts a deadline after the send', () {
      expect(
        DocumentUnit.validateSignDeadline('20/03/2026', '15/03/2026'),
        isNull,
      );
    });

    // O campo da data do envio é quem reporta o próprio erro — repetir aqui
    // marcaria os dois campos em vermelho pelo mesmo problema.
    test('only checks the format when the send date is unusable', () {
      expect(DocumentUnit.validateSignDeadline('20/03/2026', ''), isNull);
    });
  });

  group('EmployeeDocument suggested schedule date', () {
    EmployeeDocument documentSuggesting(String date) => EmployeeDocument(
          id: '1',
          name: 'Holerite',
          description: '',
          statusId: '1',
          statusName: '',
          isSignable: true,
          canGenerateDocument: true,
          usePreviousPeriod: false,
          totalUnitsCount: 0,
          units: const [],
          suggestedSignatureScheduleDate: date,
        );

    test('hasSuggestedSignatureScheduleDate reflects whether there is one', () {
      expect(
        documentSuggesting('15/03/2026').hasSuggestedSignatureScheduleDate,
        isTrue,
      );
      expect(documentSuggesting('').hasSuggestedSignatureScheduleDate, isFalse);
    });
  });

  group('EmployeeDocument competency', () {
    EmployeeDocument documentWithPeriodType(int? periodTypeId) =>
        EmployeeDocument(
          id: '1',
          name: 'Doc',
          description: '',
          statusId: '1',
          statusName: '',
          isSignable: false,
          canGenerateDocument: false,
          usePreviousPeriod: false,
          periodTypeId: periodTypeId,
          totalUnitsCount: 0,
          units: const [],
        );

    test('a document is by competency only when the template sets one', () {
      expect(documentWithPeriodType(3).isByCompetency, isTrue);
      expect(documentWithPeriodType(null).isByCompetency, isFalse);
    });

    // usePreviousPeriod é falso tanto no documento sem competência quanto no que
    // usa a corrente — por isso ele não serve para responder essa pergunta.
    test('a document that uses the current period is still by competency', () {
      const doc = EmployeeDocument(
        id: '1',
        name: 'Doc',
        description: '',
        statusId: '1',
        statusName: '',
        isSignable: false,
        canGenerateDocument: false,
        usePreviousPeriod: false,
        periodTypeId: 4,
        totalUnitsCount: 0,
        units: [],
      );
      expect(doc.isByCompetency, isTrue);
    });

    test('the granularity is resolved from the period type id', () {
      expect(documentWithPeriodType(1).periodGranularity,
          PeriodGranularity.daily);
      expect(documentWithPeriodType(3).periodGranularity,
          PeriodGranularity.monthly);
      expect(documentWithPeriodType(null).periodGranularity, isNull);
    });

    test('an unknown period type id resolves to no granularity', () {
      expect(documentWithPeriodType(99).periodGranularity, isNull);
      expect(documentWithPeriodType(99).isByCompetency, isTrue);
    });
  });
}
