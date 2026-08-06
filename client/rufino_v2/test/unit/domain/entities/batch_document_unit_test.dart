import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/batch_document_unit.dart';

void main() {
  BatchDocumentUnitItem itemWithStatus(String id, {String name = ''}) =>
      BatchDocumentUnitItem(
        documentUnitId: 'unit-1',
        documentId: 'doc-1',
        documentTemplateId: 't1',        documentTemplateName: 'T1',        documentGroupName: 'Grupo',        employeeId: 'emp-1',
        employeeName: 'Maria da Silva',
        employeeStatusId: '2',
        employeeStatusName: 'Active',
        date: '01/03/2026',
        statusId: id,
        statusName: name,
        isSignable: false,
        canGenerateDocument: false,
      );

  group('BatchDocumentUnitItem.statusLabel', () {
    test('returns the Portuguese label for every known status id', () {
      expect(itemWithStatus('1').statusLabel, 'Pendente');
      expect(itemWithStatus('2').statusLabel, 'OK');
      expect(itemWithStatus('3').statusLabel, 'Obsoleto');
      expect(itemWithStatus('4').statusLabel, 'Inválido');
      expect(itemWithStatus('5').statusLabel, 'Requer Validação');
      expect(itemWithStatus('6').statusLabel, 'Não Aplicável');
      expect(itemWithStatus('7').statusLabel, 'Aguardando Assinatura');
      expect(itemWithStatus('8').statusLabel, 'A Vencer');
    });

    test('falls back to the raw status name for an unknown status id', () {
      expect(itemWithStatus('99', name: 'Custom').statusLabel, 'Custom');
    });

    test('falls back to the raw status id when the name is also empty', () {
      expect(itemWithStatus('99').statusLabel, '99');
    });
  });
}
