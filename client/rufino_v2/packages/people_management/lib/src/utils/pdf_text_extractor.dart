/// Utility for extracting text from PDF files.
///
/// Wraps [syncfusion_flutter_pdf] to provide a simple, fail-safe interface
/// for reading text content from the first pages of a PDF document.
library;

import 'dart:typed_data';

import 'package:syncfusion_flutter_pdf/pdf.dart';

/// Extracts text content from a PDF file's raw bytes.
///
/// Reads at most the first [maxPages] pages for performance. Returns an
/// empty string when extraction fails (e.g. scanned images without a text
/// layer, corrupted files, or password-protected PDFs).
String extractTextFromPdf(Uint8List bytes, {int maxPages = 2}) {
  try {
    final document = PdfDocument(inputBytes: bytes);
    final extractor = PdfTextExtractor(document);
    final pageCount = document.pages.count.clamp(0, maxPages);
    if (pageCount == 0) {
      document.dispose();
      return '';
    }
    final text = extractor.extractText(
      startPageIndex: 0,
      endPageIndex: pageCount - 1,
    );
    document.dispose();
    return text;
  } catch (_) {
    return '';
  }
}
