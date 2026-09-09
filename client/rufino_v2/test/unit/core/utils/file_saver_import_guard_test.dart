import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Guards the conditional import that keeps file saving working on the web.
///
/// `lib/data/services/file_save_io.dart` reaches for `dart:io`'s `Platform`,
/// which throws `Unsupported operation: Platform._operatingSystem` in a
/// browser. Every consumer must name it through a conditional import that
/// offers the web implementation alongside it:
///
/// ```dart
/// import 'file_save_io.dart' if (dart.library.js_interop) 'file_save_web.dart'
///     as platform;
/// ```
///
/// A plain `import '.../file_save_io.dart';` compiles fine and even passes VM
/// tests, where `dart:io` exists — only a static scan like this catches the
/// regression before it reaches the web build.
///
/// This guard was written for `lib/core/utils/file_saver.dart`, which the D6
/// refactor renamed and moved. It kept scanning for the old path, so for months
/// it passed by finding nothing while the hole it was written for stayed open.
/// Repointing it is what makes it a guard again.
void main() {
  group('file save import guard', () {
    /// The implementation that only exists outside the browser.
    final nativeImport = RegExp(r'file_save_io\.dart');

    /// The alternative the conditional import has to offer next to it.
    final webImport = RegExp(r'file_save_web\.dart');

    test('nobody names the native implementation without the web fallback', () {
      // The two implementation files are the TARGET of the conditional import,
      // not consumers of it.
      const implementations = {
        'lib/data/services/file_save_io.dart',
        'lib/data/services/file_save_web.dart',
      };

      final offenders = <String>[];

      for (final entity in Directory('lib').listSync(recursive: true)) {
        if (entity is! File || !entity.path.endsWith('.dart')) continue;

        final normalizedPath = entity.path.replaceAll(r'\', '/');
        if (implementations.contains(normalizedPath)) continue;

        final content = entity.readAsStringSync();
        if (nativeImport.hasMatch(content) && !webImport.hasMatch(content)) {
          offenders.add(normalizedPath);
        }
      }

      expect(
        offenders,
        isEmpty,
        reason: 'These files name the native file save implementation without '
            'the web fallback, which crashes the web build with "Unsupported '
            'operation: Platform._operatingSystem". Use the conditional '
            'import instead:\n  ${offenders.join('\n  ')}',
      );
    });

    // A varredura precisa estar olhando para algo que existe: se os arquivos
    // mudarem de nome de novo, o teste acima passa por vacuidade — foi
    // exatamente assim que este guarda morreu sem ninguém notar.
    test('the files it guards still exist', () {
      expect(File('lib/data/services/file_save_io.dart').existsSync(), isTrue);
      expect(File('lib/data/services/file_save_web.dart').existsSync(), isTrue);
      expect(
        File('lib/data/services/platform_file_save_service.dart').existsSync(),
        isTrue,
      );
    });
  });
}
