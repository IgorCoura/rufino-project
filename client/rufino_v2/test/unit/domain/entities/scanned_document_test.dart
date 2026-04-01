import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/scanned_document.dart';

void main() {
  group('ScannedDocument', () {
    late Uint8List fakePage1;
    late Uint8List fakePage2;
    late Uint8List fakePdf;

    setUp(() {
      fakePage1 = Uint8List.fromList([0xFF, 0xD8, 0x01]);
      fakePage2 = Uint8List.fromList([0xFF, 0xD8, 0x02]);
      fakePdf = Uint8List.fromList([0x25, 0x50, 0x44, 0x46]);
    });

    test('exposes page count from pages list', () {
      final doc = ScannedDocument(
        pages: [fakePage1, fakePage2],
        pdfBytes: fakePdf,
        extractedText: 'João Silva',
      );

      expect(doc.pageCount, 2);
    });

    test('reports single page count correctly', () {
      final doc = ScannedDocument(
        pages: [fakePage1],
        pdfBytes: fakePdf,
        extractedText: '',
      );

      expect(doc.pageCount, 1);
    });

    test('hasExtractedText returns true when OCR text is present', () {
      final doc = ScannedDocument(
        pages: [fakePage1],
        pdfBytes: fakePdf,
        extractedText: 'Nome: Maria Souza',
      );

      expect(doc.hasExtractedText, isTrue);
    });

    test('hasExtractedText returns false when OCR text is empty', () {
      final doc = ScannedDocument(
        pages: [fakePage1],
        pdfBytes: fakePdf,
        extractedText: '',
      );

      expect(doc.hasExtractedText, isFalse);
    });

    test('stores pages and pdf bytes correctly', () {
      final doc = ScannedDocument(
        pages: [fakePage1, fakePage2],
        pdfBytes: fakePdf,
        extractedText: 'test',
      );

      expect(doc.pages, hasLength(2));
      expect(doc.pages[0], fakePage1);
      expect(doc.pages[1], fakePage2);
      expect(doc.pdfBytes, fakePdf);
      expect(doc.extractedText, 'test');
    });
  });
}
