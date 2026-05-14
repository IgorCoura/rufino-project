/// Web stub for [extractLastDocumentDate]'s OCR backend.
///
/// ML Kit text recognition does not support web; image-based extraction is
/// silently skipped. PDF extraction continues to work because it is handled
/// before this split in the public entry point.
library;

import 'dart:typed_data';

/// Always returns `null` on web — no OCR backend available.
Future<String?> recognizeImageText(Uint8List bytes) async => null;
