import 'dart:typed_data';

import 'package:rufino_v2/core/result.dart';
import 'package:rufino_v2/data/services/file_save_service.dart';

/// In-memory [FileSaveService] used by tests.
///
/// Captures the most recent file name and bytes and lets tests force a
/// save failure ([shouldFail]) or a user cancellation ([shouldCancel]).
class RecordingFileSaveService implements FileSaveService {
  String? lastFileName;
  Uint8List? lastBytes;
  int saveCallCount = 0;
  bool shouldFail = false;
  bool shouldCancel = false;

  @override
  Future<Result<FileSaveOutcome>> saveXlsx({
    required String fileName,
    required Uint8List bytes,
  }) async {
    saveCallCount++;
    lastFileName = fileName;
    lastBytes = bytes;
    if (shouldFail) {
      return Result.error(Exception('save failed'));
    }
    if (shouldCancel) {
      return const Result.success(FileSaveOutcome.cancelled);
    }
    return const Result.success(FileSaveOutcome.saved);
  }
}
