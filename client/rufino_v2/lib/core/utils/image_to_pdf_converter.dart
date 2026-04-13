/// Standalone utility for converting raw image bytes into a PDF document.
///
/// Extracted from the platform-specific [DocumentScannerService]
/// implementations so it can be reused in any context (document upload,
/// batch processing, scanning) without depending on the scanner service.
library;

import 'package:flutter/foundation.dart';
import 'package:image/image.dart' as img;
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

/// Image file extensions accepted for upload conversion.
const kSupportedImageExtensions = [
  'jpg',
  'jpeg',
  'png',
  'gif',
  'bmp',
  'webp',
  'tiff',
  'tif',
];

/// All file extensions allowed in the document upload picker.
const kAllowedUploadExtensions = ['pdf', ...kSupportedImageExtensions];

/// Whether [extension] (without dot, case-insensitive) is a supported image.
bool isImageExtension(String extension) =>
    kSupportedImageExtensions.contains(extension.toLowerCase());

/// Converts a list of raw image bytes into a single multi-page PDF.
///
/// Each entry in [imageBytesList] becomes one page whose dimensions match
/// the source image. Images that cannot be decoded are silently skipped.
///
/// All heavy work (image decoding, PDF building, and serialization) runs
/// in a background isolate via [compute] so the UI thread stays responsive.
Future<Uint8List> convertImagesToPdf(List<Uint8List> imageBytesList) {
  return compute(_buildPdfFromImages, imageBytesList);
}

/// Decodes images, builds a PDF document, and returns its bytes.
///
/// Runs entirely inside a background isolate.
Future<Uint8List> _buildPdfFromImages(List<Uint8List> imageBytesList) async {
  final document = pw.Document();

  for (final pageBytes in imageBytesList) {
    final img.Image? decoded;
    try {
      decoded = img.decodeImage(pageBytes);
    } catch (_) {
      continue;
    }
    if (decoded == null) continue;

    document.addPage(
      pw.Page(
        pageFormat: PdfPageFormat(
          decoded.width.toDouble(),
          decoded.height.toDouble(),
        ),
        build: (context) => pw.Center(
          child: pw.Image(pw.MemoryImage(pageBytes)),
        ),
      ),
    );
  }

  final bytes = await document.save();
  return Uint8List.fromList(bytes);
}
