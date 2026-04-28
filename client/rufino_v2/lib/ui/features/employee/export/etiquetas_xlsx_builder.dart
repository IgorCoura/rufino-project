import 'employee_export_row.dart';

/// Pure helpers that turn a list of [EtiquetaRow]s into the columns and rows
/// of the "Planilha Etiquetas" spreadsheet.
///
/// Keeping this logic in a stateless class lets the ViewModel stay slim and
/// makes the column order independently testable.
class EtiquetasXlsxBuilder {
  EtiquetasXlsxBuilder._();

  /// Display name of the worksheet inside the workbook.
  static const sheetName = 'Etiquetas';

  /// File name (without extension) used when saving the spreadsheet.
  static const baseFileName = 'planilha_etiquetas';

  /// The header row of the spreadsheet, in display order.
  static const headers = <String>[
    'Nome',
    'CPF',
    'Cargo',
    'Empresa',
    'CNPJ',
  ];

  /// Returns the body rows in the order expected by [headers].
  static List<List<String>> toRows(List<EtiquetaRow> rows) {
    return rows
        .map(
          (row) => <String>[
            row.name,
            row.cpf,
            row.position,
            row.companyName,
            row.cnpj,
          ],
        )
        .toList(growable: false);
  }
}
