import 'dart:js_interop';
import 'dart:typed_data';

import 'package:web/web.dart' as web;

/// Triggers a browser download for the given [bytes] with [fileName].
///
/// Uses a temporary anchor element with a blob URL — the standard approach
/// for programmatic downloads in web browsers.
Future<void> saveFile({
  required String fileName,
  required Uint8List bytes,
}) async {
  final blob = web.Blob([bytes.toJS].toJS);
  final url = web.URL.createObjectURL(blob);
  web.HTMLAnchorElement()
    ..href = url
    ..download = fileName
    ..click();
  web.URL.revokeObjectURL(url);
}
