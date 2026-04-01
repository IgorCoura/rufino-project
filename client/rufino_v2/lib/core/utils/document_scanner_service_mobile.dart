/// Mobile implementation of [DocumentScannerService] for iOS and Android.
///
/// Uses [cunning_document_scanner] for native camera-based document
/// scanning with edge detection and perspective correction,
/// [google_mlkit_text_recognition] for OCR, and the [pdf] package
/// to convert captured images into a multi-page PDF.
library;

import 'dart:io';
import 'dart:typed_data';

import 'package:cunning_document_scanner/cunning_document_scanner.dart';
import 'package:google_mlkit_text_recognition/google_mlkit_text_recognition.dart';
import 'package:image/image.dart' as img;
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import 'document_scanner_service.dart';

/// iOS/Android implementation using native scanning and ML Kit OCR.
class DocumentScannerServiceImpl implements DocumentScannerService {
  final _textRecognizer = TextRecognizer();

  @override
  bool get isPlatformSupported => Platform.isAndroid || Platform.isIOS;

  @override
  Future<List<Uint8List>?> scanPages() async {
    final imagePaths = await CunningDocumentScanner.getPictures(
      isGalleryImportAllowed: true,
    );

    if (imagePaths == null || imagePaths.isEmpty) return null;

    final pages = <Uint8List>[];
    for (final path in imagePaths) {
      final bytes = await File(path).readAsBytes();
      pages.add(bytes);
    }
    return pages;
  }

  @override
  Future<String> recognizeText(Uint8List imageBytes) async {
    // Write to a temporary file for ML Kit input.
    final tempDir = await Directory.systemTemp.createTemp('rufino_ocr_');
    final tempFile = File('${tempDir.path}/scan_page.jpg');
    await tempFile.writeAsBytes(imageBytes);

    try {
      final inputImage = InputImage.fromFilePath(tempFile.path);
      final result = await _textRecognizer.processImage(inputImage);
      return result.text;
    } finally {
      await tempDir.delete(recursive: true);
    }
  }

  @override
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) async {
    final document = pw.Document();

    for (final pageBytes in pages) {
      final decoded = img.decodeImage(pageBytes);
      if (decoded == null) continue;

      final pdfImage = pw.MemoryImage(pageBytes);

      document.addPage(
        pw.Page(
          pageFormat: PdfPageFormat(
            decoded.width.toDouble(),
            decoded.height.toDouble(),
          ),
          build: (context) => pw.Center(
            child: pw.Image(pdfImage),
          ),
        ),
      );
    }

    final bytes = await document.save();
    return Uint8List.fromList(bytes);
  }
}
