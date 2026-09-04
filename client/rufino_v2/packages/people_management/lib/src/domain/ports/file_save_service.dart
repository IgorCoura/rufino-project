import 'dart:typed_data';

import 'package:rufino_core/rufino_core.dart';

/// Whether a save actually happened or the person dismissed the dialog.
enum FileSaveOutcome {
  /// The file was written to disk (or the browser download was triggered).
  saved,

  /// The person closed the platform save-as dialog without choosing a path.
  cancelled,
}

/// Hands a file to the platform so the person can keep it.
///
/// This is a **port**: the implementation lives in the app shell, because
/// saving a file is a plugin with native behaviour per platform (a save-as
/// dialog on Windows and macOS, a native write on Android and iOS, a browser
/// download on the web, the Downloads folder on Linux). The product only
/// declares what it needs; the shell decides how it happens.
///
/// A fake implementation is what makes the export flows testable without ever
/// touching a plugin.
abstract class FileSaveService {
  /// Saves [bytes] as `<fileName>.xlsx` — [fileName] comes **without** the
  /// extension, which the implementation appends.
  ///
  /// Returns [FileSaveOutcome.cancelled] when the person dismissed the save
  /// dialog, which is not a failure and must not be reported as one.
  Future<Result<FileSaveOutcome>> saveXlsx({
    required String fileName,
    required Uint8List bytes,
  });

  /// Saves [bytes] under [fileName], which **includes** the extension.
  ///
  /// Used by the flows that produce an archive already named by the product
  /// (the batch download and the template files).
  Future<void> saveBytes({
    required String fileName,
    required Uint8List bytes,
  });
}
