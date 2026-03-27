import 'dart:convert';
import 'dart:io';

const _fileName = 'domain_errors.json';

/// Appends [entry] to the `domain_errors.json` file in the current directory.
///
/// Reads the existing array, adds the new entry, and writes the file back.
/// Creates the file with an empty array if it does not exist yet.
void appendLogEntry(Map<String, dynamic> entry) {
  try {
    final file = File(_fileName);

    List<dynamic> existing = [];
    if (file.existsSync()) {
      final content = file.readAsStringSync();
      if (content.isNotEmpty) {
        existing = jsonDecode(content) as List<dynamic>;
      }
    }

    existing.add(entry);

    const encoder = JsonEncoder.withIndent('  ');
    file.writeAsStringSync(encoder.convert(existing));
  } catch (_) {
    // Silently ignore write failures — this is a dev-only convenience log.
  }
}
