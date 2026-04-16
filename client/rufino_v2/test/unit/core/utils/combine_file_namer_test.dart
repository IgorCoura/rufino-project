import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/combine_file_namer.dart';
import 'package:rufino_v2/domain/entities/batch_download.dart';

void main() {
  group('buildCombinedFileName', () {
    test('formats standard case with date, employee, template and suffix', () {
      const unit = BatchDownloadUnit(
        documentUnitId: 'abc12345-6789-0000-0000-000000001234',
        documentId: 'doc-1',
        employeeId: 'emp-1',
        employeeName: 'Alice Silva',
        documentTemplateName: 'Holerite',
        date: '28/02/2026',
        statusId: 2,
        statusName: 'OK',
        documentGroupName: 'RH',
        hasFile: true,
      );

      final result = buildCombinedFileName(unit);

      expect(result, 'ALICE_SILVA-2026_02_28-HOLERITE-1234.PDF');
    });

    test('replaces spaces with underscores in multi-word names', () {
      const unit = BatchDownloadUnit(
        documentUnitId: 'aaaa-bbbb-cccc-dddd-eeee5678',
        documentId: 'doc-2',
        employeeId: 'emp-2',
        employeeName: 'Maria Aparecida dos Santos',
        documentTemplateName: 'Contrato de Trabalho',
        date: '01/12/2025',
        statusId: 2,
        statusName: 'OK',
        documentGroupName: 'Admissao',
        hasFile: true,
      );

      final result = buildCombinedFileName(unit);

      expect(
        result,
        'MARIA_APARECIDA_DOS_SANTOS-2025_12_01-CONTRATO_DE_TRABALHO-5678.PDF',
      );
    });

    test('handles short document unit ID', () {
      const unit = BatchDownloadUnit(
        documentUnitId: 'ab',
        documentId: 'doc-3',
        employeeId: 'emp-3',
        employeeName: 'Bob',
        documentTemplateName: 'ASO',
        date: '05/01/2026',
        statusId: 1,
        statusName: 'Pending',
        documentGroupName: 'SST',
        hasFile: true,
      );

      final result = buildCombinedFileName(unit);

      expect(result, 'BOB-2026_01_05-ASO-AB.PDF');
    });

    test('uppercases all segments including suffix', () {
      const unit = BatchDownloadUnit(
        documentUnitId: 'lower-case-id-abcd',
        documentId: 'doc-4',
        employeeId: 'emp-4',
        employeeName: 'joao',
        documentTemplateName: 'recibo',
        date: '15/06/2026',
        statusId: 2,
        statusName: 'OK',
        documentGroupName: 'Financeiro',
        hasFile: true,
      );

      final result = buildCombinedFileName(unit);

      expect(result, 'JOAO-2026_06_15-RECIBO-ABCD.PDF');
    });
  });
}
