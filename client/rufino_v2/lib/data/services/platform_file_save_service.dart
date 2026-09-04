import 'dart:typed_data';

import 'package:people_management/people_management.dart';
import 'package:rufino_core/rufino_core.dart';

import 'file_save_io.dart' if (dart.library.js_interop) 'file_save_web.dart'
    as platform;

/// The shell's implementation of [FileSaveService].
///
/// Lives here, and not in the product package, because saving a file is a
/// plugin with native behaviour per platform. The product declares the port;
/// this class is the only place that names `file_saver` and the web download.
class PlatformFileSaveService implements FileSaveService {
  @override
  Future<Result<FileSaveOutcome>> saveXlsx({
    required String fileName,
    required Uint8List bytes,
  }) async {
    try {
      return Result.success(await platform.saveXlsx(fileName, bytes));
    } catch (e) {
      return Result.error(e);
    }
  }

  @override
  Future<void> saveBytes({
    required String fileName,
    required Uint8List bytes,
  }) =>
      platform.saveBytes(fileName, bytes);
}
