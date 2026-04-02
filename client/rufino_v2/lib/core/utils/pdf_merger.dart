/// Utility for merging multiple PDF files into a single document.
///
/// Wraps [syncfusion_flutter_pdf] to combine pages from several PDFs while
/// preserving each source page's original size and content.
library;

import 'dart:typed_data';
import 'dart:ui';

import 'package:syncfusion_flutter_pdf/pdf.dart';

import '../result.dart';

/// A named pair of file name and raw bytes for a PDF file.
class PdfFileEntry {
  /// Creates a [PdfFileEntry] with the given [name] and [bytes].
  const PdfFileEntry({required this.name, required this.bytes});

  /// The original file name (e.g. `contrato.pdf`).
  final String name;

  /// The raw bytes of the PDF file.
  final Uint8List bytes;
}

/// Merges multiple PDF files into a single PDF document.
///
/// Pages are appended in the order the [files] are provided. Each source
/// page keeps its original dimensions. Returns a [Result] containing the
/// merged PDF bytes on success, or the name of the file that failed to
/// parse on error.
///
/// A single-file list returns the original bytes unchanged.
Result<Uint8List> mergePdfFiles(List<PdfFileEntry> files) {
  if (files.isEmpty) {
    return const Result.error('No files provided');
  }

  if (files.length == 1) {
    // Validate that the single file is a parseable PDF.
    try {
      PdfDocument(inputBytes: files.first.bytes).dispose();
    } catch (_) {
      return Result.error(files.first.name);
    }
    return Result.success(files.first.bytes);
  }

  // Use the first file as the base document.
  PdfDocument merged;
  try {
    merged = PdfDocument(inputBytes: files.first.bytes);
  } catch (_) {
    return Result.error(files.first.name);
  }

  try {
    for (var f = 1; f < files.length; f++) {
      final entry = files[f];
      PdfDocument source;
      try {
        source = PdfDocument(inputBytes: entry.bytes);
      } catch (_) {
        merged.dispose();
        return Result.error(entry.name);
      }

      try {
        for (var i = 0; i < source.pages.count; i++) {
          final sourcePage = source.pages[i];
          final template = sourcePage.createTemplate();
          merged.pageSettings.size = sourcePage.size;
          merged.pageSettings.margins.all = 0;
          final page = merged.pages.add();
          page.graphics.drawPdfTemplate(
            template,
            Offset.zero,
            sourcePage.size,
          );
        }
      } finally {
        source.dispose();
      }
    }

    final result = Uint8List.fromList(merged.saveSync());
    return Result.success(result);
  } finally {
    merged.dispose();
  }
}
