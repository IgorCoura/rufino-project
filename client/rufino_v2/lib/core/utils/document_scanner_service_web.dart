/// Web implementation of [DocumentScannerService].
///
/// Uses the browser camera via `getUserMedia` for page capture. OCR is
/// not available on web, so [recognizeText] always returns an empty string
/// and the user must manually select the employee in the verification dialog.
library;

import 'dart:typed_data';

import 'package:image/image.dart' as img;
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import 'document_scanner_service.dart';

/// Web implementation that supports camera capture but not OCR.
///
/// Scanning on web is handled through the [DocumentScanDialog] widget
/// which provides a camera preview and manual page capture. This service
/// only provides [imagesToPdf] conversion and reports OCR as unavailable.
class DocumentScannerServiceImpl implements DocumentScannerService {
  @override
  bool get isPlatformSupported => true;

  /// Not used on web — page capture is handled by [DocumentScanDialog].
  ///
  /// Returns `null` because the web flow uses the dialog widget directly
  /// instead of this method.
  @override
  Future<List<Uint8List>?> scanPages() async => null;

  /// OCR is not supported on web.
  ///
  /// Returns an empty string. The user must manually select the employee
  /// in the bulk upload verification dialog.
  @override
  Future<String> recognizeText(Uint8List imageBytes) async => '';

  @override
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) async {
    final document = pw.Document();

    for (final pageBytes in pages) {
      final decoded = img.decodeImage(pageBytes);
      if (decoded == null) continue;

      final pdfImage = pw.MemoryImage(pageBytes);

      document.addPage(
        pw.Page(
          pageFormat: PdfPageFormat(
            decoded.width.toDouble(),
            decoded.height.toDouble(),
          ),
          build: (context) => pw.Center(
            child: pw.Image(pdfImage),
          ),
        ),
      );
    }

    final bytes = await document.save();
    return Uint8List.fromList(bytes);
  }
}
