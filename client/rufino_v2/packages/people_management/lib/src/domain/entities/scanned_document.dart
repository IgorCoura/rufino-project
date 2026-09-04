/// A document created by scanning physical pages with the device camera.
///
/// Holds the raw page images, the generated PDF bytes, and any text
/// extracted via OCR. Used as an intermediate representation before
/// the scanned content is matched to an employee and staged for upload.
library;

import 'dart:typed_data';

/// A scanned document composed of one or more photographed pages.
///
/// Each page is stored as JPEG bytes in [pages]. The [pdfBytes] contain
/// a single PDF with all pages combined, and [extractedText] holds the
/// concatenated OCR output from all pages (empty when OCR is unavailable,
/// e.g. on web).
class ScannedDocument {
  /// Creates a [ScannedDocument] from scanned page images.
  ///
  /// The [pages] list must contain at least one image. The [pdfBytes]
  /// represent the multi-page PDF generated from the images, and
  /// [extractedText] is the OCR result across all pages.
  const ScannedDocument({
    required this.pages,
    required this.pdfBytes,
    required this.extractedText,
  });

  /// The raw JPEG bytes for each scanned page.
  final List<Uint8List> pages;

  /// The combined PDF document generated from all [pages].
  final Uint8List pdfBytes;

  /// Text extracted via OCR from all pages, concatenated with spaces.
  ///
  /// Empty when OCR is not available on the current platform (e.g. web).
  final String extractedText;

  /// The number of pages in this scanned document.
  int get pageCount => pages.length;

  /// Whether OCR text was successfully extracted from the document.
  bool get hasExtractedText => extractedText.isNotEmpty;
}
