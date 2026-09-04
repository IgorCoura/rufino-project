import 'dart:js_interop';
import 'dart:typed_data';

import 'package:file_saver/file_saver.dart';
import 'package:people_management/people_management.dart';
import 'package:web/web.dart' as web;

/// Triggers a browser download of [bytes] as `<fileName>.xlsx`.
Future<FileSaveOutcome> saveXlsx(String fileName, Uint8List bytes) async {
  await FileSaver.instance.saveFile(
    name: fileName,
    bytes: bytes,
    ext: 'xlsx',
    mimeType: MimeType.microsoftExcel,
  );
  return FileSaveOutcome.saved;
}

/// Triggers a browser download of [bytes] under [fileName].
///
/// Uses a temporary anchor with a blob URL — the standard way to start a
/// programmatic download in a browser.
Future<void> saveBytes(String fileName, Uint8List bytes) async {
  final blob = web.Blob([bytes.toJS].toJS);
  final url = web.URL.createObjectURL(blob);
  web.HTMLAnchorElement()
    ..href = url
    ..download = fileName
    ..click();
  web.URL.revokeObjectURL(url);
}
