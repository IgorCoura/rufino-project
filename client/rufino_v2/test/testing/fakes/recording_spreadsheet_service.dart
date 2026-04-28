import 'dart:typed_data';

import 'package:rufino_v2/data/services/spreadsheet_service.dart';

/// Captures the arguments passed to [SpreadsheetService.buildXlsx] without
/// touching the heavy `xlsio` workbook. Returns a fixed two-byte payload
/// that starts with the ZIP signature so callers that sniff the header
/// still see a "valid" file.
class RecordingSpreadsheetService implements SpreadsheetService {
  String? lastSheetName;
  List<String>? lastHeaders;
  List<List<String>>? lastRows;
  int callCount = 0;

  @override
  Uint8List buildXlsx({
    required String sheetName,
    required List<String> headers,
    required List<List<String>> rows,
  }) {
    callCount++;
    lastSheetName = sheetName;
    lastHeaders = headers;
    lastRows = rows;
    return Uint8List.fromList(const [0x50, 0x4B, 0x03, 0x04]);
  }
}
