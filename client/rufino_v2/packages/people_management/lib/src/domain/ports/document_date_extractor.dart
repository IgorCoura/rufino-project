import 'dart:typed_data';

/// Reads the date stamped on a document file.
///
/// This is a **port**: PDFs are read with a pure Dart parser, but an image only
/// gives up its text through OCR, which is a plugin with native code. The shell
/// supplies the implementation; the product only asks the question.
///
/// Returns the date as `dd/MM/yyyy`, or `null` when the file type is not
/// supported, no date is found, or extraction fails. It never throws — a
/// document whose date cannot be read is a document the person fills in by
/// hand, not an error to report.
typedef DocumentDateExtractor = Future<String?> Function({
  required Uint8List bytes,
  required String fileName,
});
