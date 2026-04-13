/// Native (non-web) implementation of [mergeAndReadBytes].
///
/// Writes the merged PDF to a temporary file, reads it back as bytes,
/// and cleans up the file afterwards.
library;

import 'dart:io';
import 'dart:typed_data';

import 'package:pdf_combiner/models/merge_input.dart';
import 'package:pdf_combiner/pdf_combiner.dart';

/// Merges [inputs] into a single PDF and returns the raw bytes.
Future<Uint8List> mergeAndReadBytes(List<MergeInput> inputs) async {
  final outputPath =
      '${Directory.systemTemp.path}/merged_${DateTime.now().millisecondsSinceEpoch}.pdf';

  try {
    await PdfCombiner.mergeMultiplePDFs(
      inputs: inputs,
      outputPath: outputPath,
    );

    return await File(outputPath).readAsBytes();
  } finally {
    final file = File(outputPath);
    if (file.existsSync()) {
      file.deleteSync();
    }
  }
}
