import 'dart:io' show File, Platform;
import 'dart:typed_data';

import 'package:file_saver/file_saver.dart';
import 'package:people_management/people_management.dart';

/// Saves [bytes] as `<fileName>.xlsx` on a platform with a file system.
///
/// Windows, macOS, Android and iOS open the native "save as" dialog. Linux
/// falls back to the Downloads directory, because `file_saver` has no save
/// dialog there.
Future<FileSaveOutcome> saveXlsx(String fileName, Uint8List bytes) async {
  if (Platform.isWindows ||
      Platform.isMacOS ||
      Platform.isAndroid ||
      Platform.isIOS) {
    final path = await FileSaver.instance.saveAs(
      name: fileName,
      bytes: bytes,
      ext: '.xlsx',
      mimeType: MimeType.microsoftExcel,
    );
    if (path == null || path.isEmpty) return FileSaveOutcome.cancelled;
    // The Windows plugin only opens the dialog and returns the chosen path —
    // the bytes have to be written by us. macOS, iOS and Android persist the
    // file natively.
    if (Platform.isWindows) {
      await File(path).writeAsBytes(bytes, flush: true);
    }
    return FileSaveOutcome.saved;
  }

  await FileSaver.instance.saveFile(
    name: fileName,
    bytes: bytes,
    ext: '.xlsx',
    mimeType: MimeType.microsoftExcel,
  );
  return FileSaveOutcome.saved;
}

/// Saves [bytes] under [fileName], which already carries its extension.
Future<void> saveBytes(String fileName, Uint8List bytes) async {
  final dot = fileName.lastIndexOf('.');
  final base = dot > 0 ? fileName.substring(0, dot) : fileName;
  final ext = dot > 0 ? fileName.substring(dot + 1) : '';

  if (Platform.isWindows ||
      Platform.isMacOS ||
      Platform.isAndroid ||
      Platform.isIOS) {
    final path = await FileSaver.instance.saveAs(
      name: base,
      bytes: bytes,
      ext: ext,
      mimeType: MimeType.other,
    );
    if (path == null || path.isEmpty) return;
    if (Platform.isWindows) {
      await File(path).writeAsBytes(bytes, flush: true);
    }
    return;
  }

  await FileSaver.instance.saveFile(
    name: base,
    bytes: bytes,
    ext: ext,
    mimeType: MimeType.other,
  );
}

/// Writes [bytes] to [path] — the path came from the platform's save dialog.
Future<void> writeToPath(String path, Uint8List bytes) =>
    File(path).writeAsBytes(bytes, flush: true);
