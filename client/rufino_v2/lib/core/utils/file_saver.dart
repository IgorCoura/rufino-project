import 'dart:io';

/// Writes [bytes] to a file at [path] on platforms that support `dart:io`.
///
/// On desktop, [FilePicker.platform.saveFile] only returns a path — the
/// caller must write the bytes manually via this helper.
Future<void> writeFileToPath(String path, List<int> bytes) async {
  await File(path).writeAsBytes(bytes);
}
