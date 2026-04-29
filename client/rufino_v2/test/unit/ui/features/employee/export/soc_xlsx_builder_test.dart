import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/employee/export/employee_export_row.dart';
import 'package:rufino_v2/ui/features/employee/export/soc_xlsx_builder.dart';

void main() {
  group('SocXlsxBuilder', () {
    test('exposes 38 columns in the SOC template order', () {
      expect(SocXlsxBuilder.headers, hasLength(38));
      expect(SocXlsxBuilder.headers.first, 'Nome Unidade');
      expect(SocXlsxBuilder.headers[3], 'Nome Funcionário');
      expect(SocXlsxBuilder.headers.last, 'Matrícula RH');
    });

    test('renders each row with the same column count as the headers', () {
      final rows = SocXlsxBuilder.toRows(const [
        SocRow(
          employeeName: 'Maria Souza',
          cpf: '123.456.789-09',
          status: 'Ativo',
          unitCorporateName: 'Acme Ltda',
        ),
      ]);

      expect(rows, hasLength(1));
      expect(rows.first.length, SocXlsxBuilder.headers.length);
    });

    test('places SocRow fields on the same indices as the matching headers',
        () {
      final rows = SocXlsxBuilder.toRows(const [
        SocRow(
          employeeName: 'Maria Souza',
          cpf: '123.456.789-09',
          status: 'Ativo',
          unitCorporateName: 'Acme Ltda',
          hrRegistration: '0042',
        ),
      ]);
      final row = rows.first;

      expect(row[SocXlsxBuilder.headers.indexOf('Nome Funcionário')],
          'Maria Souza');
      expect(row[SocXlsxBuilder.headers.indexOf('CPF')], '123.456.789-09');
      expect(row[SocXlsxBuilder.headers.indexOf('Situação')], 'Ativo');
      expect(row[SocXlsxBuilder.headers.indexOf('Razão Social Unid.')],
          'Acme Ltda');
      expect(
          row[SocXlsxBuilder.headers.indexOf('Matrícula RH')], '0042');
    });

    test('emits empty strings for fields that are not provided', () {
      final rows = SocXlsxBuilder.toRows(const [
        SocRow(employeeName: 'Maria Souza'),
      ]);

      final row = rows.first;
      expect(row[SocXlsxBuilder.headers.indexOf('Rg')], '');
      expect(row[SocXlsxBuilder.headers.indexOf('CBO')], '');
      expect(row[SocXlsxBuilder.headers.indexOf('Grau de Risco')], '');
    });
  });
}
