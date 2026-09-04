import 'dart:typed_data';

/// Scans a document with the camera and reads what is written on it.
///
/// This is a **port**: the implementations live in the app shell, because
/// scanning needs the camera, a runtime permission and an OCR engine — three
/// plugins with native code and entries in the platform manifests. The product
/// declares the capability; the shell decides how it is provided, and answers
/// [isPlatformSupported] with `false` where it is not.
///
/// The concrete implementation is chosen at compile time by the shell's
/// conditional import; this contract carries no factory of its own precisely so
/// that the product never names an implementation.
abstract class DocumentScannerService {
  /// Whether document scanning is available on the current platform.
  bool get isPlatformSupported;

  /// Opens the native document scanner (mobile) or camera (web).
  ///
  /// Returns the page images as JPEG bytes, or `null` when the person
  /// cancelled.
  Future<List<Uint8List>?> scanPages();

  /// Extracts text from a scanned page image using OCR.
  ///
  /// Returns an empty string on platforms without OCR support.
  Future<String> recognizeText(Uint8List imageBytes);

  /// Converts page images into a single multi-page PDF document.
  Future<Uint8List> imagesToPdf(List<Uint8List> pages);

  /// Opens the system settings page for this app.
  ///
  /// The way out when a permission was denied permanently: the product can
  /// only point there, and asking again would do nothing.
  Future<void> openAppSettings();
}
