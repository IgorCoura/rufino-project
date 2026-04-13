import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/pdf_merger.dart';
import 'package:syncfusion_flutter_pdf/pdf.dart';

/// Creates a valid PDF with the given [pageCount] pages, each containing
/// visible text so they survive the save/reload cycle.
PdfFileEntry _createTestPdf({
  required String name,
  int pageCount = 1,
}) {
  final doc = PdfDocument();
  final font = PdfStandardFont(PdfFontFamily.helvetica, 12);
  for (var i = 0; i < pageCount; i++) {
    doc.pages.add().graphics.drawString('Page ${i + 1}', font);
  }
  final bytes = Uint8List.fromList(doc.saveSync());
  doc.dispose();
  return PdfFileEntry(name: name, bytes: bytes);
}

void main() {
  group('mergePdfFiles', () {
    test('returns error when the file list is empty', () async {
      final result = await mergePdfFiles([]);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, equals('No files provided'));
    });

    test('returns the original bytes when given a single file', () async {
      final entry = _createTestPdf(name: 'single.pdf', pageCount: 2);

      final result = await mergePdfFiles([entry]);

      expect(result.isSuccess, isTrue);
      expect(result.valueOrNull, same(entry.bytes));
      final doc = PdfDocument(inputBytes: result.valueOrNull!);
      expect(doc.pages.count, equals(2));
      doc.dispose();
    });

    test('returns error with file name when a file is corrupted', () async {
      final valid = _createTestPdf(name: 'good.pdf');
      final corrupted = PdfFileEntry(
        name: 'bad_file.pdf',
        bytes: Uint8List.fromList([0, 1, 2, 3, 4]),
      );

      final result = await mergePdfFiles([valid, corrupted]);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, equals('bad_file.pdf'));
    });

    test(
      'returns error with file name when the corrupted file is first',
      () async {
        final corrupted = PdfFileEntry(
          name: 'broken.pdf',
          bytes: Uint8List.fromList([99, 99, 99]),
        );
        final valid = _createTestPdf(name: 'ok.pdf');

        final result = await mergePdfFiles([corrupted, valid]);

        expect(result.isError, isTrue);
        expect(result.errorOrNull, equals('broken.pdf'));
      },
    );

    test('returns error for a single corrupted file', () async {
      final corrupted = PdfFileEntry(
        name: 'invalid.pdf',
        bytes: Uint8List.fromList([1, 2, 3]),
      );

      final result = await mergePdfFiles([corrupted]);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, equals('invalid.pdf'));
    });
  });
}
