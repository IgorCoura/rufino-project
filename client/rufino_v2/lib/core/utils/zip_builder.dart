import 'dart:typed_data';

import 'package:archive/archive.dart';

/// A named pair of file name and raw bytes for a ZIP entry.
typedef ZipEntry = ({String fileName, Uint8List bytes});

/// Builds a ZIP archive in memory from the given [entries].
///
/// Each entry becomes a file inside the archive, stored with fastest
/// compression for speed. Returns the raw ZIP bytes.
Uint8List buildZipFromEntries(List<ZipEntry> entries) {
  final archive = Archive();
  for (final entry in entries) {
    archive.addFile(
      ArchiveFile.bytes(entry.fileName, entry.bytes),
    );
  }
  final encoded = ZipEncoder().encode(archive);
  return Uint8List.fromList(encoded);
}
