import 'dart:typed_data';

import 'package:syncfusion_flutter_xlsio/xlsio.dart';

/// A stateless service that builds `.xlsx` workbooks from tabular data.
///
/// Wraps the `syncfusion_flutter_xlsio` package so the rest of the app depends
/// only on this thin contract and remains easy to mock in tests.
class SpreadsheetService {
  /// Creates an in-memory `.xlsx` workbook with a single worksheet.
  ///
  /// [sheetName] is the visible tab name. [headers] are written to row 1 in
  /// bold; [rows] follow starting at row 2. Every cell is written as text so
  /// the consumer never has to deal with type coercion (e.g. CPFs preserve
  /// leading zeros).
  ///
  /// Returns the encoded workbook bytes ready to be saved to disk.
  Uint8List buildXlsx({
    required String sheetName,
    required List<String> headers,
    required List<List<String>> rows,
  }) {
    final workbook = Workbook();
    try {
      final sheet = workbook.worksheets[0];
      sheet.name = sheetName;

      for (var col = 0; col < headers.length; col++) {
        final cell = sheet.getRangeByIndex(1, col + 1);
        cell.setText(headers[col]);
        cell.cellStyle.bold = true;
      }

      for (var rowIndex = 0; rowIndex < rows.length; rowIndex++) {
        final row = rows[rowIndex];
        for (var col = 0; col < row.length; col++) {
          sheet.getRangeByIndex(rowIndex + 2, col + 1).setText(row[col]);
        }
      }

      final bytes = workbook.saveAsStream();
      return Uint8List.fromList(bytes);
    } finally {
      workbook.dispose();
    }
  }
}
