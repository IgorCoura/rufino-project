/// Utility for merging multiple PDF files into a single document.
///
/// Uses [pdf_combiner] with native platform engines (PDFBox on Android,
/// PDFium on Windows/Linux, native Swift on iOS/macOS, PDFLib on web) for
/// lossless merging that preserves each page's original size, orientation,
/// and content.
library;

import 'dart:typed_data';

import 'package:pdf_combiner/models/merge_input.dart';
import 'package:syncfusion_flutter_pdf/pdf.dart';

import '../result.dart';
import 'pdf_merger_io.dart' if (dart.library.js_interop) 'pdf_merger_web.dart'
    as platform;

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
/// page keeps its original dimensions, orientation, and content intact.
/// Returns a [Result] containing the merged PDF bytes on success, or the
/// name of the file that failed to parse on error.
///
/// A single-file list returns the original bytes unchanged.
Future<Result<Uint8List>> mergePdfFiles(List<PdfFileEntry> files) async {
  if (files.isEmpty) {
    return const Result.error('No files provided');
  }

  // Validate all files are parseable PDFs before attempting the merge.
  for (final entry in files) {
    try {
      PdfDocument(inputBytes: entry.bytes).dispose();
    } catch (_) {
      return Result.error(entry.name);
    }
  }

  if (files.length == 1) {
    return Result.success(files.first.bytes);
  }

  try {
    final inputs = files.map((f) => MergeInput.bytes(f.bytes)).toList();
    final mergedBytes = await platform.mergeAndReadBytes(inputs);
    return Result.success(mergedBytes);
  } catch (e) {
    return Result.error(e.toString());
  }
}
