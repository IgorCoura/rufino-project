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

/// Maximum length (in pixels) of the longest side of an image embedded in
/// the generated PDF.
///
/// Document scanners (especially Apple VisionKit) can produce pages above
/// 3000 px on the long side. Anything beyond ~2000 px adds file size with
/// no readability gain on printed documents, so pages above this threshold
/// are downscaled before being embedded.
const kPdfPageMaxLongSide = 2000;

/// JPEG quality used when re-encoding image pages for the generated PDF.
///
/// 75 is the standard sweet spot for document scans: visually
/// indistinguishable from the original at viewing distance, while shrinking
/// each page to roughly 200–400 KB. Keeps a 17-page scan well under the
/// backend's 10 MB upload limit.
const kPdfPageJpegQuality = 75;

/// Whether [extension] (without dot, case-insensitive) is a supported image.
bool isImageExtension(String extension) =>
    kSupportedImageExtensions.contains(extension.toLowerCase());

/// Converts a list of raw image bytes into a single multi-page PDF.
///
/// Each entry in [imageBytesList] becomes one page. Pages whose longest
/// side exceeds [kPdfPageMaxLongSide] are downscaled, and every page is
/// re-encoded as JPEG at [kPdfPageJpegQuality] before being embedded — this
/// keeps the output under the backend's 10 MB upload cap even for long
/// scans. Images that cannot be decoded are silently skipped.
///
/// All heavy work (image decoding, resizing, JPEG encoding, PDF building,
/// and serialization) runs in a background isolate via [compute] so the UI
/// thread stays responsive.
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

    final normalized = _normalizeForPdf(decoded);
    final encoded = Uint8List.fromList(
      img.encodeJpg(normalized, quality: kPdfPageJpegQuality),
    );

    document.addPage(
      pw.Page(
        pageFormat: PdfPageFormat(
          normalized.width.toDouble(),
          normalized.height.toDouble(),
        ),
        build: (context) => pw.Center(
          child: pw.Image(pw.MemoryImage(encoded)),
        ),
      ),
    );
  }

  final bytes = await document.save();
  return Uint8List.fromList(bytes);
}

/// Returns [image] scaled down so its longest side is at most
/// [kPdfPageMaxLongSide]; returns the original when already within limits.
img.Image _normalizeForPdf(img.Image image) {
  final longSide = image.width >= image.height ? image.width : image.height;
  if (longSide <= kPdfPageMaxLongSide) return image;

  if (image.width >= image.height) {
    return img.copyResize(image, width: kPdfPageMaxLongSide);
  }
  return img.copyResize(image, height: kPdfPageMaxLongSide);
}
