import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/employee_document.dart';

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

  group('EmployeeDocument.groupStatusLabel', () {
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

    test('returns OK for status id 1', () {
      expect(docWithStatus('1').groupStatusLabel, 'OK');
    });

    test('returns Pendente for status id 2', () {
      expect(docWithStatus('2').groupStatusLabel, 'Pendente');
    });

    test('returns Inválido for status id 3', () {
      expect(docWithStatus('3').groupStatusLabel, 'Inválido');
    });

    test('returns statusName as fallback for unknown status id', () {
      expect(docWithStatus('99', name: 'Custom').groupStatusLabel, 'Custom');
    });

    test('returns raw status id when statusName is also empty', () {
      expect(docWithStatus('99').groupStatusLabel, '99');
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

    test('returns statusName as fallback for unknown status id', () {
      expect(unitWithStatus('99', name: 'Custom').statusLabel, 'Custom');
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
}
