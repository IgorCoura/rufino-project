/// Platform-abstracted document scanning service.
///
/// Provides camera-based document scanning, OCR text recognition,
/// and image-to-PDF conversion. Platform implementations are selected
/// at compile time via conditional imports.
library;

import 'dart:typed_data';

import 'document_scanner_service_stub.dart'
    if (dart.library.js_interop) 'document_scanner_service_web.dart'
    if (dart.library.io) 'document_scanner_service_mobile.dart'
    as platform;

/// Contract for document scanning operations.
///
/// Platform implementations handle camera access, image processing,
/// OCR, and PDF creation. Desktop platforms return unsupported.
abstract class DocumentScannerService {
  /// Creates the platform-appropriate [DocumentScannerService].
  factory DocumentScannerService() = platform.DocumentScannerServiceImpl;

  /// Whether document scanning is available on the current platform.
  bool get isPlatformSupported;

  /// Opens the native document scanner (mobile) or camera (web).
  ///
  /// Returns a list of page images as JPEG bytes, or `null` if the
  /// user cancelled the operation.
  Future<List<Uint8List>?> scanPages();

  /// Extracts text from a scanned page image using OCR.
  ///
  /// Returns an empty string on platforms without OCR support (web, desktop).
  Future<String> recognizeText(Uint8List imageBytes);

  /// Converts a list of page images into a single multi-page PDF document.
  ///
  /// Each image in [pages] becomes one page in the resulting PDF.
  Future<Uint8List> imagesToPdf(List<Uint8List> pages);
}
