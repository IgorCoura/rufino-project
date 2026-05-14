/// Utility for extracting a date stamp from a document file (PDF or image).
///
/// PDFs are read via [extractTextFromPdf]; images go through OCR when
/// running on a platform that supports ML Kit. Returns the **last** valid
/// `dd/MM/yyyy` date found, since contracts typically place the signature
/// date near the end of the document.
library;

import 'dart:typed_data';

import 'package:flutter/foundation.dart' show visibleForTesting;

import 'document_date_extractor_io.dart'
    if (dart.library.html) 'document_date_extractor_web.dart' as ocr;
import 'pdf_text_extractor.dart';

/// Returns the last valid `dd/MM/yyyy` date found inside the document.
///
/// Returns `null` when the file type is unsupported, no date is found, or
/// extraction fails for any reason. Never throws.
///
/// [bytes] are the raw file contents and [fileName] is used only to
/// disambiguate between PDF and image flows by extension.
Future<String?> extractLastDocumentDate({
  required Uint8List bytes,
  required String fileName,
}) async {
  final lower = fileName.toLowerCase();
  if (lower.endsWith('.pdf')) {
    final text = extractTextFromPdf(bytes, maxPages: 100);
    if (text.isEmpty) return null;
    return findLastValidDate(text);
  }
  if (lower.endsWith('.png') ||
      lower.endsWith('.jpg') ||
      lower.endsWith('.jpeg')) {
    final text = await ocr.recognizeImageText(bytes);
    if (text == null || text.isEmpty) return null;
    return findLastValidDate(text);
  }
  return null;
}

/// Returns the last `dd/MM/yyyy` substring in [text] that represents a real
/// calendar date (month 1–12, day 1–31, year 1900–2100), or `null` if none.
///
/// Exposed for unit testing — the public entry point is
/// [extractLastDocumentDate].
@visibleForTesting
String? findLastValidDate(String text) {
  final matches = RegExp(r'\b(\d{2})/(\d{2})/(\d{4})\b').allMatches(text);
  String? last;
  for (final m in matches) {
    final day = int.parse(m.group(1)!);
    final month = int.parse(m.group(2)!);
    final year = int.parse(m.group(3)!);
    if (year < 1900 || year > 2100) continue;
    if (month < 1 || month > 12) continue;
    if (day < 1 || day > 31) continue;
    last = '${m.group(1)}/${m.group(2)}/${m.group(3)}';
  }
  return last;
}
