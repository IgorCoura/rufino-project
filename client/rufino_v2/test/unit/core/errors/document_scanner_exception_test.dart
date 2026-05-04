import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/errors/document_scanner_exception.dart';
import 'package:rufino_v2/core/errors/expected_failure.dart';

void main() {
  group('DocumentScannerException', () {
    test('is a sealed Exception subtype', () {
      const exception = ScannerPermissionDeniedException();
      expect(exception, isA<DocumentScannerException>());
      expect(exception, isA<Exception>());
    });

    test('marks permission-denied variants as ExpectedFailure so they '
        'are filtered out of the crash dashboard', () {
      expect(
        const ScannerPermissionDeniedException(),
        isA<ExpectedFailure>(),
      );
      expect(
        const ScannerPermissionPermanentlyDeniedException(),
        isA<ExpectedFailure>(),
      );
    });

    test('does not mark plugin-failure variants as ExpectedFailure so they '
        'do reach the crash dashboard', () {
      expect(
        ScannerPluginFailureException(Exception('boom')),
        isNot(isA<ExpectedFailure>()),
      );
      expect(
        ScannerFileReadException('/tmp/page.jpg', Exception('io error')),
        isNot(isA<ExpectedFailure>()),
      );
    });

    test('ScannerPluginFailureException carries the underlying cause', () {
      final cause = Exception('native plugin failed');
      final exception = ScannerPluginFailureException(cause);

      expect(exception.cause, same(cause));
    });

    test('ScannerFileReadException carries the failing path and the cause',
        () {
      final cause = FileSystemException('not found');
      final exception = ScannerFileReadException('/tmp/page.jpg', cause);

      expect(exception.path, '/tmp/page.jpg');
      expect(exception.cause, same(cause));
    });
  });
}

class FileSystemException implements Exception {
  FileSystemException(this.message);
  final String message;
}
