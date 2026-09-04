import 'dart:typed_data';

import 'package:people_management/people_management.dart';

/// In-memory [FilePickerService] used by tests.
///
/// Defaults to "the person cancelled" — an empty list — so a test that does not
/// care about picking never opens anything. Set [filesToReturn] for the flows
/// that do.
class FakeFilePickerService implements FilePickerService {
  /// What the next [pickFiles] hands back.
  List<PickedFile> filesToReturn = const [];

  /// What the next [chooseSavePath] hands back; null means cancelled.
  String? savePathToReturn;

  /// Arguments of the last [pickFiles] call, for assertions.
  bool? lastAllowMultiple;
  List<String>? lastAllowedExtensions;

  /// Paths written through [writeToPath], in order.
  final List<({String path, Uint8List bytes})> written = [];

  @override
  Future<List<PickedFile>> pickFiles({
    bool allowMultiple = false,
    List<String>? allowedExtensions,
  }) async {
    lastAllowMultiple = allowMultiple;
    lastAllowedExtensions = allowedExtensions;
    return filesToReturn;
  }

  @override
  Future<String?> chooseSavePath({
    required String dialogTitle,
    required String fileName,
  }) async =>
      savePathToReturn;

  @override
  Future<void> writeToPath({
    required String path,
    required Uint8List bytes,
  }) async {
    written.add((path: path, bytes: bytes));
  }
}
