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
    test('returns error when the file list is empty', () {
      final result = mergePdfFiles([]);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, equals('No files provided'));
    });

    test('returns the original bytes when given a single file', () {
      final entry = _createTestPdf(name: 'single.pdf', pageCount: 2);

      final result = mergePdfFiles([entry]);

      expect(result.isSuccess, isTrue);
      expect(result.valueOrNull, same(entry.bytes));
      final doc = PdfDocument(inputBytes: result.valueOrNull!);
      expect(doc.pages.count, equals(2));
      doc.dispose();
    });

    test('combines pages from multiple files in order', () {
      final file1 = _createTestPdf(name: 'a.pdf', pageCount: 2);
      final file2 = _createTestPdf(name: 'b.pdf', pageCount: 3);
      final file3 = _createTestPdf(name: 'c.pdf', pageCount: 1);

      final result = mergePdfFiles([file1, file2, file3]);

      expect(result.isSuccess, isTrue);
      final merged = PdfDocument(inputBytes: result.valueOrNull!);
      expect(merged.pages.count, equals(6));
      merged.dispose();
    });

    test('returns error with file name when a file is corrupted', () {
      final valid = _createTestPdf(name: 'good.pdf');
      final corrupted = PdfFileEntry(
        name: 'bad_file.pdf',
        bytes: Uint8List.fromList([0, 1, 2, 3, 4]),
      );

      final result = mergePdfFiles([valid, corrupted]);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, equals('bad_file.pdf'));
    });

    test(
      'returns error with file name when the corrupted file is first',
      () {
        final corrupted = PdfFileEntry(
          name: 'broken.pdf',
          bytes: Uint8List.fromList([99, 99, 99]),
        );
        final valid = _createTestPdf(name: 'ok.pdf');

        final result = mergePdfFiles([corrupted, valid]);

        expect(result.isError, isTrue);
        expect(result.errorOrNull, equals('broken.pdf'));
      },
    );

    test('returns error for a single corrupted file', () {
      final corrupted = PdfFileEntry(
        name: 'invalid.pdf',
        bytes: Uint8List.fromList([1, 2, 3]),
      );

      final result = mergePdfFiles([corrupted]);

      expect(result.isError, isTrue);
      expect(result.errorOrNull, equals('invalid.pdf'));
    });

    test('produces a result that is itself a parseable PDF', () {
      final file1 = _createTestPdf(name: 'x.pdf', pageCount: 1);
      final file2 = _createTestPdf(name: 'y.pdf', pageCount: 1);

      final result = mergePdfFiles([file1, file2]);

      expect(result.isSuccess, isTrue);
      final doc = PdfDocument(inputBytes: result.valueOrNull!);
      expect(doc.pages.count, greaterThan(0));
      doc.dispose();
    });
  });
}
