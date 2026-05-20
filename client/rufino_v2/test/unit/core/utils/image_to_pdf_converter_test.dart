import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:image/image.dart' as img;
import 'package:rufino_v2/core/utils/image_to_pdf_converter.dart';
import 'package:syncfusion_flutter_pdf/pdf.dart';

/// Creates a minimal valid PNG image with the given dimensions.
Uint8List _createTestImage({int width = 100, int height = 80}) {
  final image = img.Image(width: width, height: height);
  img.fill(image, color: img.ColorRgb8(200, 100, 50));
  return Uint8List.fromList(img.encodePng(image));
}

void main() {
  group('convertImagesToPdf', () {
    test('converts a single image into a one-page PDF', () async {
      final imageBytes = _createTestImage();

      final pdfBytes = await convertImagesToPdf([imageBytes]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      expect(doc.pages.count, equals(1));
      doc.dispose();
    });

    test('converts multiple images into a multi-page PDF', () async {
      final images = [
        _createTestImage(width: 100, height: 80),
        _createTestImage(width: 200, height: 150),
        _createTestImage(width: 50, height: 50),
      ];

      final pdfBytes = await convertImagesToPdf(images);

      final doc = PdfDocument(inputBytes: pdfBytes);
      expect(doc.pages.count, equals(3));
      doc.dispose();
    });

    test('skips undecodable image bytes without crashing', () async {
      final validImage = _createTestImage();
      final invalidBytes = Uint8List.fromList([0, 1, 2, 3, 4]);

      final pdfBytes = await convertImagesToPdf([
        validImage,
        invalidBytes,
      ]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      expect(doc.pages.count, equals(1));
      doc.dispose();
    });

    test('returns a valid empty PDF when given an empty list', () async {
      final pdfBytes = await convertImagesToPdf([]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      expect(doc.pages.count, equals(0));
      doc.dispose();
    });

    test('returns a valid empty PDF when all images are undecodable',
        () async {
      final garbage1 = Uint8List.fromList([1, 2, 3]);
      final garbage2 = Uint8List.fromList([4, 5, 6]);

      final pdfBytes = await convertImagesToPdf([garbage1, garbage2]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      expect(doc.pages.count, equals(0));
      doc.dispose();
    });

    test('downscales pages whose longest side exceeds the PDF cap', () async {
      final oversizedLandscape = _createTestImage(
        width: kPdfPageMaxLongSide * 2,
        height: kPdfPageMaxLongSide,
      );

      final pdfBytes = await convertImagesToPdf([oversizedLandscape]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      expect(doc.pages.count, equals(1));
      final pageSize = doc.pages[0].size;
      expect(pageSize.width.round(), equals(kPdfPageMaxLongSide));
      expect(pageSize.height.round(), equals(kPdfPageMaxLongSide ~/ 2));
      doc.dispose();
    });

    test('downscales portrait pages by capping the height', () async {
      final oversizedPortrait = _createTestImage(
        width: kPdfPageMaxLongSide,
        height: kPdfPageMaxLongSide * 2,
      );

      final pdfBytes = await convertImagesToPdf([oversizedPortrait]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      final pageSize = doc.pages[0].size;
      expect(pageSize.height.round(), equals(kPdfPageMaxLongSide));
      expect(pageSize.width.round(), equals(kPdfPageMaxLongSide ~/ 2));
      doc.dispose();
    });

    test('keeps dimensions when image is already within the PDF cap',
        () async {
      final small = _createTestImage(width: 800, height: 600);

      final pdfBytes = await convertImagesToPdf([small]);

      final doc = PdfDocument(inputBytes: pdfBytes);
      final pageSize = doc.pages[0].size;
      expect(pageSize.width.round(), equals(800));
      expect(pageSize.height.round(), equals(600));
      doc.dispose();
    });

    test(
      'output stays under the 10 MB upload cap for many oversized pages',
      () async {
        // Simulates a long scan (17 pages at VisionKit-sized PNGs) — the
        // exact scenario that previously blew past the backend's 10 MB cap.
        final page = _createTestImage(
          width: kPdfPageMaxLongSide * 2,
          height: kPdfPageMaxLongSide * 2,
        );
        final pages = List<Uint8List>.generate(17, (_) => page);

        final pdfBytes = await convertImagesToPdf(pages);

        expect(pdfBytes.lengthInBytes, lessThan(10 * 1024 * 1024));
      },
      timeout: const Timeout(Duration(minutes: 3)),
    );
  });

  group('isImageExtension', () {
    test('returns true for supported image extensions', () {
      for (final ext in ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp',
          'tiff', 'tif']) {
        expect(isImageExtension(ext), isTrue, reason: 'expected $ext');
      }
    });

    test('returns false for pdf extension', () {
      expect(isImageExtension('pdf'), isFalse);
    });

    test('returns false for unknown extensions', () {
      expect(isImageExtension('docx'), isFalse);
      expect(isImageExtension('txt'), isFalse);
    });

    test('is case-insensitive', () {
      expect(isImageExtension('JPG'), isTrue);
      expect(isImageExtension('Png'), isTrue);
      expect(isImageExtension('JPEG'), isTrue);
    });
  });

  group('kAllowedUploadExtensions', () {
    test('includes pdf and all supported image extensions', () {
      expect(kAllowedUploadExtensions, contains('pdf'));
      for (final ext in kSupportedImageExtensions) {
        expect(kAllowedUploadExtensions, contains(ext));
      }
    });
  });
}
