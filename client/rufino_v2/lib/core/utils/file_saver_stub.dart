/// Stub for platforms where `dart:io` is unavailable (web).
///
/// On web, [FilePicker.platform.saveFile] handles the download via the
/// `bytes` parameter — no manual file write is needed.
Future<void> writeFileToPath(String path, List<int> bytes) async {
  // No-op on web.
}
