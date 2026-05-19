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
import 'package:permission_handler/permission_handler.dart';

import '../errors/document_scanner_exception.dart';
import 'document_scanner_service.dart';
import 'image_to_pdf_converter.dart';

/// iOS/Android implementation using native scanning and ML Kit OCR.
class DocumentScannerServiceImpl implements DocumentScannerService {
  final _textRecognizer = TextRecognizer();

  @override
  bool get isPlatformSupported => Platform.isAndroid || Platform.isIOS;

  @override
  Future<List<Uint8List>?> scanPages() async {
    await _ensureCameraPermission();

    final List<String>? imagePaths;
    try {
      imagePaths = await CunningDocumentScanner.getPictures(
        isGalleryImportAllowed: true,
        // PNG (plugin default on iOS) is encoded synchronously on the main
        // thread via libpng+zlib, which hangs the UI for ~150–300 ms per page
        // at VisionKit's native resolution. JPEG is 5–10× faster and Android
        // already hard-codes JPEG, so this also aligns the two platforms.
        iosScannerOptions: const IosScannerOptions(
          imageFormat: IosImageFormat.jpg,
          jpgCompressionQuality: 0.8,
        ),
      );
    } on DocumentScannerException {
      rethrow;
    } catch (e) {
      throw ScannerPluginFailureException(e);
    }

    if (imagePaths == null || imagePaths.isEmpty) return null;

    final pages = <Uint8List>[];
    for (final path in imagePaths) {
      try {
        pages.add(await File(path).readAsBytes());
      } catch (e) {
        throw ScannerFileReadException(path, e);
      }
    }
    return pages;
  }

  /// Requests camera permission and throws a typed scanner exception when
  /// it cannot be granted, so the native scanner is never invoked without
  /// authorization (iOS silently no-ops the presentation in that case,
  /// which is the root cause of "tapping Digitalizar does nothing").
  Future<void> _ensureCameraPermission() async {
    final status = await Permission.camera.request();
    if (status.isGranted || status.isLimited) return;
    if (status.isPermanentlyDenied || status.isRestricted) {
      throw const ScannerPermissionPermanentlyDeniedException();
    }
    throw const ScannerPermissionDeniedException();
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
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) =>
      convertImagesToPdf(pages);
}
