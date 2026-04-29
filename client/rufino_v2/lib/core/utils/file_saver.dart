import 'dart:io';
import 'dart:typed_data';

import 'package:file_saver/file_saver.dart';

/// Saves [bytes] to a user-chosen location.
///
/// On Windows and macOS, opens the native "save as" dialog and writes the
/// bytes to the chosen path. On Android and iOS, the platform plugin
/// persists the file natively. On Linux, falls back to the Downloads
/// directory because the save-as dialog is not implemented there.
///
/// [fileName] should include the extension (e.g. `documentos.zip`). The
/// extension is parsed and forwarded to the underlying plugin so the system
/// applies the correct MIME type.
Future<void> saveFile({
  required String fileName,
  required Uint8List bytes,
}) async {
  final dotIndex = fileName.lastIndexOf('.');
  final baseName = dotIndex > 0 ? fileName.substring(0, dotIndex) : fileName;
  final ext = dotIndex > 0 ? fileName.substring(dotIndex + 1) : '';

  if (Platform.isWindows ||
      Platform.isMacOS ||
      Platform.isAndroid ||
      Platform.isIOS) {
    final path = await FileSaver.instance.saveAs(
      name: baseName,
      bytes: bytes,
      ext: ext,
      mimeType: MimeType.other,
    );
    if (path == null || path.isEmpty) return;
    // The Windows plugin only opens the dialog and returns the chosen path —
    // the bytes still need to be written by us. The other platforms persist
    // the file natively.
    if (Platform.isWindows) {
      await File(path).writeAsBytes(bytes, flush: true);
    }
    return;
  }

  // Linux fallback — saveAs is not implemented, so write directly to the
  // user's Downloads directory.
  await FileSaver.instance.saveFile(
    name: baseName,
    bytes: bytes,
    ext: ext,
    mimeType: MimeType.other,
  );
}
