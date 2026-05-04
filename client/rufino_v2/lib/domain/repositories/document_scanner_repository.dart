import 'dart:typed_data';

import '../../core/result.dart';

/// Domain contract for the document scanner aggregate.
///
/// All fallible operations return [Result] so callers are forced by the
/// type system to handle both success and failure outcomes — no
/// exception propagates across the layer boundary.
abstract class DocumentScannerRepository {
  /// Whether the current platform can present the native scanner.
  bool get isPlatformSupported;

  /// Opens the native document scanner and returns the captured pages
  /// as JPEG byte buffers.
  ///
  /// Returns `Result.success(null)` when the user cancelled the scan
  /// (a normal control-flow outcome, not an error).
  /// Returns `Result.error` carrying a `DocumentScannerException`
  /// subtype when the scanner could not run (permission denied, plugin
  /// failure, file system error).
  Future<Result<List<Uint8List>?>> scanPages();

  /// Extracts text from a scanned page image using the OCR engine.
  ///
  /// Returns an empty string on platforms without OCR support.
  Future<String> recognizeText(Uint8List imageBytes);

  /// Converts a list of page images into a single multi-page PDF
  /// document.
  Future<Uint8List> imagesToPdf(List<Uint8List> pages);
}
