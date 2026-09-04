import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:people_management/people_management.dart';

import 'file_save_io.dart' if (dart.library.js_interop) 'file_save_web.dart'
    as platform;

/// The shell's implementation of [FilePickerService].
///
/// Lives here because `file_picker` is a plugin with native code. This is the
/// only place in the app that names it, besides the bill payment module's own
/// picker — the two contracts are deliberately separate: that one hands back a
/// single document with its content type, this one takes many files filtered by
/// extension. A shared abstraction would have to satisfy both and would end up
/// describing neither.
class PlatformFilePickerService implements FilePickerService {
  @override
  Future<List<PickedFile>> pickFiles({
    bool allowMultiple = false,
    List<String>? allowedExtensions,
  }) async {
    final result = await FilePicker.platform.pickFiles(
      withData: true,
      allowMultiple: allowMultiple,
      type: allowedExtensions == null ? FileType.any : FileType.custom,
      allowedExtensions: allowedExtensions,
    );
    if (result == null) return const [];
    return [
      for (final f in result.files) (name: f.name, bytes: f.bytes),
    ];
  }

  @override
  Future<String?> chooseSavePath({
    required String dialogTitle,
    required String fileName,
  }) =>
      FilePicker.platform.saveFile(
        dialogTitle: dialogTitle,
        fileName: fileName,
      );

  @override
  Future<void> writeToPath({
    required String path,
    required Uint8List bytes,
  }) =>
      platform.writeToPath(path, bytes);
}
