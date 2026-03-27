import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';

/// Opens a native save-file dialog and writes [bytes] to the chosen path.
///
/// On desktop/mobile, [FilePicker.platform.saveFile] returns a file path
/// and the bytes must be written manually.
Future<void> saveFile({
  required String fileName,
  required Uint8List bytes,
}) async {
  final savePath = await FilePicker.platform.saveFile(
    dialogTitle: 'Salvar arquivo',
    fileName: fileName,
  );
  if (savePath != null) {
    await File(savePath).writeAsBytes(bytes);
  }
}
