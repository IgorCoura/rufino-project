import 'dart:convert';

import 'package:archive/archive.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/services/spreadsheet_service.dart';

String _readEntry(Archive archive, String path) {
  final file = archive.findFile(path);
  expect(file, isNotNull, reason: 'expected $path inside xlsx');
  return utf8.decode(file!.content as List<int>);
}

void main() {
  group('SpreadsheetService.buildXlsx', () {
    final service = SpreadsheetService();

    test('produces a valid xlsx archive starting with the zip signature', () {
      final bytes = service.buildXlsx(
        sheetName: 'Test',
        headers: const ['A', 'B'],
        rows: const [
          ['1', '2'],
        ],
      );

      expect(bytes, isNotEmpty);
      // .xlsx files are zip archives — they always start with PK\x03\x04.
      expect(bytes[0], 0x50);
      expect(bytes[1], 0x4B);
    });

    test('writes the worksheet name into the workbook part', () {
      final bytes = service.buildXlsx(
        sheetName: 'Pessoas',
        headers: const ['Nome'],
        rows: const [
          ['Maria'],
        ],
      );

      final archive = ZipDecoder().decodeBytes(bytes);
      final workbookXml = _readEntry(archive, 'xl/workbook.xml');
      expect(workbookXml, contains('name="Pessoas"'));
    });

    test('persists header and row text into the shared strings table', () {
      final bytes = service.buildXlsx(
        sheetName: 'Pessoas',
        headers: const ['Nome', 'CPF'],
        rows: const [
          ['Maria', '123.456.789-09'],
          ['João', '987.654.321-00'],
        ],
      );

      final archive = ZipDecoder().decodeBytes(bytes);
      final sharedStrings = _readEntry(archive, 'xl/sharedStrings.xml');
      expect(sharedStrings, contains('Nome'));
      expect(sharedStrings, contains('CPF'));
      expect(sharedStrings, contains('Maria'));
      expect(sharedStrings, contains('123.456.789-09'));
      expect(sharedStrings, contains('João'));
      expect(sharedStrings, contains('987.654.321-00'));
    });

    test('still produces a valid file when there are zero data rows', () {
      final bytes = service.buildXlsx(
        sheetName: 'Empty',
        headers: const ['Header'],
        rows: const [],
      );

      expect(bytes, isNotEmpty);
      final archive = ZipDecoder().decodeBytes(bytes);
      expect(archive.findFile('xl/workbook.xml'), isNotNull);
    });
  });
}
