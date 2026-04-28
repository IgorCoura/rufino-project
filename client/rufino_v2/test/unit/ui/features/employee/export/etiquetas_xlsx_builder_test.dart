import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/employee/export/employee_export_row.dart';
import 'package:rufino_v2/ui/features/employee/export/etiquetas_xlsx_builder.dart';

void main() {
  group('EtiquetasXlsxBuilder', () {
    test('exposes the five required column headers in display order', () {
      expect(
        EtiquetasXlsxBuilder.headers,
        const ['Nome', 'CPF', 'Cargo', 'Empresa', 'CNPJ'],
      );
    });

    test('preserves column order when converting rows', () {
      final rows = EtiquetasXlsxBuilder.toRows(const [
        EtiquetaRow(
          name: 'João Silva',
          cpf: '111.444.777-35',
          position: 'Analista',
          companyName: 'Acme Ltda',
          cnpj: '12.345.678/0001-90',
        ),
      ]);

      expect(rows, hasLength(1));
      expect(rows.first, [
        'João Silva',
        '111.444.777-35',
        'Analista',
        'Acme Ltda',
        '12.345.678/0001-90',
      ]);
    });

    test('returns an empty list when there are no employees', () {
      expect(EtiquetasXlsxBuilder.toRows(const []), isEmpty);
    });
  });
}
