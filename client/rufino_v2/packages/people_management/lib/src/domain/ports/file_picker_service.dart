import 'dart:typed_data';

/// A file the person chose in the system picker.
///
/// [bytes] is null when the platform handed back a path without reading the
/// file — callers that need the content must treat that as "not chosen".
typedef PickedFile = ({String name, Uint8List? bytes});

/// Opens the system file picker and the "save as" dialog.
///
/// This is a **port**: `file_picker` is a plugin with native code and platform
/// permissions, so the implementation lives in the app shell. The product says
/// what it wants — one file or many, of which extensions — and never names the
/// plugin.
///
/// A fake implementation is what lets the upload flows be tested without ever
/// opening a native dialog.
abstract class FilePickerService {
  /// Opens the picker and returns what was chosen, empty when cancelled.
  ///
  /// [allowedExtensions] filters by extension (without the dot); omitting it
  /// accepts anything. The content is always read, because every caller in
  /// this product uploads the bytes.
  Future<List<PickedFile>> pickFiles({
    bool allowMultiple = false,
    List<String>? allowedExtensions,
  });

  /// Asks where to save a file, returning the chosen path or null.
  ///
  /// Returns null on platforms with no save dialog, which callers must treat
  /// as a cancellation rather than an error.
  Future<String?> chooseSavePath({
    required String dialogTitle,
    required String fileName,
  });

  /// Writes [bytes] to a path previously returned by [chooseSavePath].
  ///
  /// Lives on the port because writing to an arbitrary path is `dart:io`, and
  /// the product does not import it.
  Future<void> writeToPath({
    required String path,
    required Uint8List bytes,
  });
}
