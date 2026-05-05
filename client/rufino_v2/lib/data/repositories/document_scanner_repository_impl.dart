import 'dart:typed_data';

import '../../core/errors/document_scanner_exception.dart';
import '../../core/monitoring/error_reporter.dart';
import '../../core/result.dart';
import '../../core/utils/document_scanner_service.dart';
import '../../domain/repositories/document_scanner_repository.dart';

/// Concrete [DocumentScannerRepository] backed by [DocumentScannerService].
///
/// Wraps the service's `scanPages` call in `try/catch`, reports
/// unexpected errors to [ErrorReporter] and returns a typed
/// [Result]. Errors marked as `ExpectedFailure` (e.g. user denied a
/// permission) are short-circuited inside [ErrorReporter.capture] and
/// therefore never reach the crash dashboard.
class DocumentScannerRepositoryImpl implements DocumentScannerRepository {
  DocumentScannerRepositoryImpl({
    required this.scannerService,
    required this.reporter,
  });

  final DocumentScannerService scannerService;
  final ErrorReporter reporter;

  @override
  bool get isPlatformSupported => scannerService.isPlatformSupported;

  @override
  Future<Result<List<Uint8List>?>> scanPages() async {
    try {
      final pages = await scannerService.scanPages();
      return Result.success(pages);
    } on DocumentScannerException catch (e, st) {
      return reporter.failure(e, st, context: _scannerContext(e));
    } catch (e, st) {
      return reporter.failure(
        ScannerPluginFailureException(e),
        st,
        context: {'op': 'scanPages', 'cause': e.toString()},
      );
    }
  }

  /// Builds a structured context map for Sentry that surfaces the native
  /// plugin's underlying cause without leaking PII.
  Map<String, Object?> _scannerContext(DocumentScannerException e) =>
      switch (e) {
        ScannerPluginFailureException(:final cause) => {
            'op': 'scanPages',
            'cause': cause.toString(),
          },
        ScannerFileReadException(:final path, :final cause) => {
            'op': 'scanPages',
            'path': path,
            'cause': cause.toString(),
          },
        ScannerPermissionDeniedException() ||
        ScannerPermissionPermanentlyDeniedException() =>
          const {'op': 'scanPages'},
      };

  @override
  Future<String> recognizeText(Uint8List imageBytes) =>
      scannerService.recognizeText(imageBytes);

  @override
  Future<Uint8List> imagesToPdf(List<Uint8List> pages) =>
      scannerService.imagesToPdf(pages);
}
