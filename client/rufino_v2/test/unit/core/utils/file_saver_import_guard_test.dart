import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Guards against importing the native [file_saver.dart] without a web fallback.
///
/// `lib/core/utils/file_saver.dart` reaches for `dart:io`'s `Platform`, which
/// throws `Unsupported operation: Platform._operatingSystem` on the web. Every
/// consumer must import it through a conditional import that defaults to
/// `file_saver_stub.dart` (the browser blob-download implementation) and only
/// switches to the native file under `if (dart.library.io)`:
///
/// ```dart
/// import '.../core/utils/file_saver_stub.dart'
///     if (dart.library.io) '.../core/utils/file_saver.dart';
/// ```
///
/// A plain `import '.../core/utils/file_saver.dart';` compiles fine and even
/// passes VM tests (where `dart:io` exists), so only a static scan like this
/// catches the regression before it ships to the web build.
void main() {
  group('file_saver import guard', () {
    /// The local native import, regardless of the relative `../` depth.
    final nativeImport = RegExp(r'core/utils/file_saver\.dart');

    /// The local stub import that the conditional default must point to.
    final stubImport = RegExp(r'core/utils/file_saver_stub\.dart');

    test('every consumer of the native file saver also imports the web stub',
        () {
      // The implementation files themselves are exempt — they are the targets
      // of the conditional import, not consumers of it.
      const implementationFiles = {
        'lib/core/utils/file_saver.dart',
        'lib/core/utils/file_saver_stub.dart',
      };

      final offenders = <String>[];

      for (final entity in Directory('lib').listSync(recursive: true)) {
        if (entity is! File || !entity.path.endsWith('.dart')) continue;

        final normalizedPath = entity.path.replaceAll(r'\', '/');
        if (implementationFiles.contains(normalizedPath)) continue;

        final content = entity.readAsStringSync();
        final importsNative = nativeImport.hasMatch(content);
        final importsStub = stubImport.hasMatch(content);

        if (importsNative && !importsStub) {
          offenders.add(normalizedPath);
        }
      }

      expect(
        offenders,
        isEmpty,
        reason: 'These files import the native file_saver.dart directly, which '
            'crashes on the web with "Unsupported operation: '
            'Platform._operatingSystem". Use a conditional import defaulting to '
            'file_saver_stub.dart instead:\n  ${offenders.join('\n  ')}',
      );
    });
  });
}
