import 'dart:io' show File, Platform;
import 'dart:typed_data';

import 'package:file_saver/file_saver.dart';
import 'package:flutter/foundation.dart' show kIsWeb;

import '../../core/result.dart';

/// Returned by [FileSaveService.saveXlsx] to differentiate a real save from
/// the user dismissing the platform's "save as" dialog.
enum FileSaveOutcome {
  /// The file was saved to disk (or the browser download was triggered).
  saved,

  /// The user closed the platform save-as dialog without choosing a path.
  cancelled,
}

/// A stateless service that prompts the platform to save a file locally.
///
/// Behaviour per platform:
/// * Web — triggers a browser download via the `file_saver` web plugin.
/// * Windows, macOS, Android, iOS — opens the native "save as" dialog so
///   the user picks a location and confirms the action.
/// * Linux — falls back to writing into the user's Downloads directory
///   because `file_saver` does not implement a Linux save dialog.
///
/// Wrapped here so callers depend on a single contract that can be mocked
/// in tests without invoking the underlying platform plugin.
class FileSaveService {
  /// Saves [bytes] as `<fileName>.xlsx`.
  ///
  /// [fileName] must be provided without an extension — the `.xlsx`
  /// extension is appended internally. Returns [FileSaveOutcome.saved] when
  /// the file was written and [FileSaveOutcome.cancelled] when the user
  /// dismissed the save dialog.
  Future<Result<FileSaveOutcome>> saveXlsx({
    required String fileName,
    required Uint8List bytes,
  }) async {
    try {
      if (kIsWeb) {
        await FileSaver.instance.saveFile(
          name: fileName,
          bytes: bytes,
          ext: 'xlsx',
          mimeType: MimeType.microsoftExcel,
        );
        return const Result.success(FileSaveOutcome.saved);
      }

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
        if (path == null || path.isEmpty) {
          return const Result.success(FileSaveOutcome.cancelled);
        }
        // The Windows plugin only opens the "Save As" dialog and returns the
        // chosen path — the bytes have to be written by us. macOS, iOS and
        // Android plugins already persist the file natively.
        if (Platform.isWindows) {
          await File(path).writeAsBytes(bytes, flush: true);
        }
        return const Result.success(FileSaveOutcome.saved);
      }

      // Linux fallback — saveAs is not implemented, so write directly to
      // the Downloads directory.
      await FileSaver.instance.saveFile(
        name: fileName,
        bytes: bytes,
        ext: '.xlsx',
        mimeType: MimeType.microsoftExcel,
      );
      return const Result.success(FileSaveOutcome.saved);
    } catch (e) {
      return Result.error(e);
    }
  }
}
