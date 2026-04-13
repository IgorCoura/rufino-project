/// Web implementation of [mergeAndReadBytes].
///
/// On web, [PdfCombiner] returns a blob URL. This implementation fetches
/// the blob contents and returns them as bytes.
library;

import 'dart:js_interop';
import 'dart:typed_data';

import 'package:pdf_combiner/models/merge_input.dart';
import 'package:pdf_combiner/pdf_combiner.dart';
import 'package:web/web.dart' as web;

/// Merges [inputs] into a single PDF and returns the raw bytes.
Future<Uint8List> mergeAndReadBytes(List<MergeInput> inputs) async {
  final blobUrl = await PdfCombiner.mergeMultiplePDFs(
    inputs: inputs,
    outputPath: 'merged.pdf',
  );

  try {
    final response = await web.window.fetch(blobUrl.toJS).toDart;
    final arrayBuffer = await response.arrayBuffer().toDart;
    return arrayBuffer.toDart.asUint8List();
  } finally {
    web.URL.revokeObjectURL(blobUrl);
  }
}
