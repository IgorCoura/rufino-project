import 'dart:typed_data';

// ignore: avoid_web_libraries_in_flutter
import 'dart:html' as html;

/// Triggers a browser download for the given [bytes] with [fileName].
///
/// Uses a temporary anchor element with a blob URL — the standard approach
/// for programmatic downloads in web browsers.
Future<void> saveFile({
  required String fileName,
  required Uint8List bytes,
}) async {
  final blob = html.Blob([bytes]);
  final url = html.Url.createObjectUrlFromBlob(blob);
  html.AnchorElement(href: url)
    ..setAttribute('download', fileName)
    ..click();
  html.Url.revokeObjectUrl(url);
}
